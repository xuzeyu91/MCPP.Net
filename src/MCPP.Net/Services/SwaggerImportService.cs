using MCPP.Net.Core;
using MCPP.Net.Models;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MCPP.Net.Services
{
    /// <summary>
    /// Swagger导入服务
    /// </summary>
    public class SwaggerImportService
    {
        private readonly ILogger<SwaggerImportService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ImportedToolsService _importedToolsService;
        private readonly IAssemblyBuilder _assemblyBuilder;
        private readonly IToolAssemblyLoader _toolAssemblyLoader;
        private readonly string _storageDirectory;
        private readonly string _assemblyDirectory;

        public SwaggerImportService(
            ILogger<SwaggerImportService> logger,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment hostEnvironment,
            ImportedToolsService importedToolsService,
            IAssemblyBuilder assemblyBuilder,
            IToolAssemblyLoader toolAssemblyLoader)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _hostEnvironment = hostEnvironment;
            _importedToolsService = importedToolsService;
            _assemblyBuilder = assemblyBuilder;
            _toolAssemblyLoader = toolAssemblyLoader;
            _storageDirectory = Path.Combine(_hostEnvironment.ContentRootPath, "ImportedSwaggers");
            _assemblyDirectory = _storageDirectory; // 使用相同目录存储程序集

            // 确保存储目录存在
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        /// <summary>
        /// 导入Swagger文档并生成MCP工具
        /// </summary>
        /// <param name="request">导入请求</param>
        /// <returns>导入结果</returns>
        public async Task<SwaggerImportResult> ImportSwaggerAsync(SwaggerImportRequest request)
        {
            _logger.LogInformation("开始处理Swagger导入: {Url}", request.SwaggerUrl);

            // 1. 获取Swagger JSON内容
            string swaggerJson = await GetSwaggerJsonAsync(request.SwaggerUrl);
            if (string.IsNullOrEmpty(swaggerJson))
            {
                throw new Exception($"无法获取Swagger文档: {request.SwaggerUrl}");
            }

            // 2. 解析Swagger JSON
            JObject swaggerDoc = JObject.Parse(swaggerJson);

            // 获取服务器基础URL
            string baseUrl = "";
            // 优先使用用户提供的源服务器URL
            if (!string.IsNullOrEmpty(request.SourceBaseUrl))
            {
                baseUrl = request.SourceBaseUrl;
                _logger.LogInformation("使用用户提供的源服务器URL: {BaseUrl}", baseUrl);
            }
            else if (swaggerDoc["servers"] != null && swaggerDoc["servers"]!.Type == JTokenType.Array)
            {
                JArray servers = (JArray)swaggerDoc["servers"]!;
                if (servers.Count > 0 && servers[0]["url"] != null)
                {
                    baseUrl = servers[0]["url"]!.ToString();
                    _logger.LogInformation("从Swagger文档中获取服务器URL: {BaseUrl}", baseUrl);
                }
            }

            _toolAssemblyLoader.Unload($"{request.NameSpace}.{request.ClassName}");

            var assemblyPath = _assemblyBuilder.Build(swaggerDoc, request, baseUrl);

            var loadedDetail = _toolAssemblyLoader.Load(assemblyPath);

            foreach (var importedTool in loadedDetail.ImportedTools)
            {
                // 将工具信息保存到ImportedToolsService，确保程序重启后能自动加载
                _importedToolsService.AddImportedTool(importedTool);
            }

            // 6. 返回导入结果
            return new SwaggerImportResult
            {
                Success = true,
                ApiCount = loadedDetail.RegisteredMethods.Count,
                ToolClassName = request.ClassName,
                ImportedApis = loadedDetail.RegisteredMethods
            };
        }

        /// <summary>
        /// 获取Swagger JSON内容
        /// </summary>
        /// <param name="swaggerUrlOrPath">Swagger URL或本地文件路径</param>
        /// <returns>Swagger JSON内容</returns>
        private async Task<string> GetSwaggerJsonAsync(string swaggerUrlOrPath)
        {
            // 判断是否为URL
            if (Uri.TryCreate(swaggerUrlOrPath, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // 通过HTTP请求获取Swagger文档
                var httpClient = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await httpClient.GetAsync(swaggerUrlOrPath);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                // 从本地文件读取Swagger文档
                if (!File.Exists(swaggerUrlOrPath))
                {
                    throw new FileNotFoundException($"找不到Swagger文件: {swaggerUrlOrPath}");
                }
                return await File.ReadAllTextAsync(swaggerUrlOrPath);
            }
        }

        /// <summary>
        /// 获取所有已导入的工具
        /// </summary>
        /// <returns>已导入的工具列表</returns>
        public IReadOnlyList<ImportedTool> GetImportedTools()
        {
            return _importedToolsService.GetImportedTools();
        }

        /// <summary>
        /// 获取所有已加载的动态工具类型
        /// </summary>
        /// <returns>已加载的工具类型列表</returns>
        public List<Type> GetDynamicToolTypes()
        {
            List<Type> toolTypes = new List<Type>();

            try
            {
                // 只搜索DLL文件
                var assemblyFiles = Directory.GetFiles(_assemblyDirectory, "*.dll");
                foreach (var file in assemblyFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        // 检查是否以命名空间.类名开头(然后是下划线和时间戳)
                        int underscoreIndex = fileName.IndexOf('_');
                        if (underscoreIndex == -1) continue; // 跳过不符合命名规则的DLL
                        
                        string fullName = fileName.Substring(0, underscoreIndex);
                        int lastDotIndex = fullName.LastIndexOf('.');
                        if (lastDotIndex == -1) continue;

                        string nameSpace = fullName.Substring(0, lastDotIndex);
                        string className = fullName.Substring(lastDotIndex + 1);

                        // 加载程序集
                        try
                        {
                            Assembly assembly = Assembly.LoadFrom(file);
                            Type? toolType = assembly.GetType($"{nameSpace}.{className}");

                            if (toolType != null)
                            {
                                toolTypes.Add(toolType);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "加载已有程序集失败: {Path}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "加载工具类型失败: {File}, {Message}", Path.GetFileName(file), ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取动态工具类型失败: {Message}", ex.Message);
            }

            return toolTypes;
        }

        /// <summary>
        /// 删除已导入的工具
        /// </summary>
        /// <param name="nameSpace">工具命名空间</param>
        /// <param name="className">类名</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteImportedTool(string nameSpace, string className)
        {
            string key = $"{nameSpace}.{className}";

            // 检查工具是否存在
            if (!_importedToolsService.GetImportedTools().Any(t => t.NameSpace == nameSpace && t.ClassName == className))
            {
                _logger.LogWarning("尝试删除不存在的工具: {Key}", key);
                return false;
            }

            try
            {
                var filePath = Path.Combine(_assemblyDirectory, $"{key}.dll");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("已删除工具程序集文件: {FilePath}", filePath);
                }

                // 从ImportedToolsService中移除工具信息
                _importedToolsService.RemoveImportedTool(nameSpace, className);

                _logger.LogInformation("已成功删除工具: {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除工具失败: {Key}, {Message}", key, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void CleanupResources()
        {
            // 清理已加载的程序集   
            foreach (var context in AssemblyLoadContext.All)
            {
                if (context != AssemblyLoadContext.Default)
                {
                    context.Unload();
                }
            }
        }
    }
}