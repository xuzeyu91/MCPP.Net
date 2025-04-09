using MCPP.Net.Models;
using ModelContextProtocol.Server;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MCPP.Net.Services
{
    /// <summary>
    /// 已导入工具服务
    /// </summary>
    public class ImportedToolsService
    {
        private readonly ILogger<ImportedToolsService> _logger;
        private readonly IMcpServerMethodRegistry _methodRegistry;
        private readonly string _toolsStoragePath;
        private readonly string _swaggerDllPath;
        private readonly List<ImportedTool> _importedTools = new List<ImportedTool>();

        public ImportedToolsService(
            ILogger<ImportedToolsService> logger,
            IMcpServerMethodRegistry methodRegistry,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _methodRegistry = methodRegistry;
            _toolsStoragePath = Path.Combine(hostEnvironment.ContentRootPath, "ImportedTools");
            _swaggerDllPath = Path.Combine(hostEnvironment.ContentRootPath, "ImportedSwaggers");
            
            // 创建导入工具存储目录
            if (!Directory.Exists(_toolsStoragePath))
            {
                Directory.CreateDirectory(_toolsStoragePath);
            }
            
            // 加载已导入的工具信息
            LoadImportedTools();
            
            // 加载ImportedSwaggers目录中的DLL
            LoadSwaggerDllTools();
        }

        /// <summary>
        /// 获取已导入的工具列表
        /// </summary>
        public IReadOnlyList<ImportedTool> GetImportedTools()
        {
            return _importedTools.AsReadOnly();
        }

        /// <summary>
        /// 添加导入的工具信息
        /// </summary>
        public void AddImportedTool(ImportedTool tool)
        {
            _importedTools.Add(tool);
            SaveImportedTools();
        }

        /// <summary>
        /// 根据命名空间和类名加载已编译的工具
        /// </summary>
        public bool LoadCompiledTool(string nameSpace, string className)
        {
            try
            {
                var assemblyPath = Path.Combine(_toolsStoragePath, $"{nameSpace}.{className}.dll");
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogError("找不到工具程序集: {AssemblyPath}", assemblyPath);
                    return false;
                }

                // 加载程序集
                var assembly = Assembly.LoadFrom(assemblyPath);
                var toolType = assembly.GetType($"{nameSpace}.{className}");
                
                if (toolType == null)
                {
                    _logger.LogError("找不到工具类型: {NameSpace}.{ClassName}", nameSpace, className);
                    return false;
                }

                // 获取所有带有McpServerTool特性的方法
                var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

                _methodRegistry.Clear();
                foreach (var method in methods)
                {
                    _methodRegistry.AddMethod(method);
                }

                _logger.LogInformation("成功加载工具: {NameSpace}.{ClassName}", nameSpace, className);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载工具失败: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从ImportedSwaggers目录加载DLL工具
        /// </summary>
        public void LoadSwaggerDllTools()
        {
            if (!Directory.Exists(_swaggerDllPath))
            {
                _logger.LogInformation("ImportedSwaggers目录不存在，跳过加载");
                return;
            }

            _logger.LogInformation("开始从ImportedSwaggers目录加载DLL工具");
            
            try
            {
                // 获取ImportedSwaggers目录中的所有DLL文件
                var dllFiles = Directory.GetFiles(_swaggerDllPath, "*.dll");
                _logger.LogInformation("在ImportedSwaggers目录中找到 {Count} 个DLL文件", dllFiles.Length);

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        // 加载程序集
                        var assembly = Assembly.LoadFrom(dllFile);
                        _logger.LogInformation("已加载程序集: {AssemblyName}", assembly.GetName().Name);
                        
                        // 获取程序集中所有类型
                        var types = assembly.GetTypes();
                        
                        foreach (var type in types)
                        {
                            // 查找带有McpServerToolType特性的类型
                            var toolTypeAttr = type.GetCustomAttribute<McpServerToolTypeAttribute>();
                            if (toolTypeAttr != null)
                            {
                                _logger.LogInformation("找到工具类型: {TypeName}", type.FullName);
                                
                                // 获取所有带有McpServerTool特性的方法
                                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
                                    .ToList();
                                
                                if (methods.Count > 0)
                                {
                                    // 为每个找到的方法注册到MCP服务
                                    foreach (var method in methods)
                                    {
                                        _methodRegistry.AddMethod(method);
                                    }
                                    
                                    _logger.LogInformation("已注册 {Count} 个方法, 来自 {TypeName}", 
                                        methods.Count, type.FullName);
                                    
                                    // 添加到导入工具列表，方便管理
                                    var importedTool = new ImportedTool
                                    {
                                        NameSpace = type.Namespace ?? "UnknownNamespace",
                                        ClassName = type.Name,
                                        ApiCount = methods.Count,
                                        ImportDate = DateTime.Now,
                                        SwaggerSource = "ImportedSwaggers目录自动加载",
                                        SourceBaseUrl = ""
                                    };
                                    
                                    // 只有当列表中不存在相同命名空间和类名的工具时才添加
                                    if (!_importedTools.Any(t => t.NameSpace == importedTool.NameSpace && 
                                                              t.ClassName == importedTool.ClassName))
                                    {
                                        _importedTools.Add(importedTool);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "加载DLL文件失败: {FileName}, {Message}", 
                            Path.GetFileName(dllFile), ex.Message);
                    }
                }
                
                // 保存更新后的工具信息
                SaveImportedTools();
                _logger.LogInformation("完成从ImportedSwaggers目录加载DLL工具");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从ImportedSwaggers目录加载DLL工具失败: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// 保存工具信息到文件
        /// </summary>
        private void SaveImportedTools()
        {
            var json = JsonSerializer.Serialize(_importedTools, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(_toolsStoragePath, "imported_tools.json");
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 从文件加载工具信息
        /// </summary>
        private void LoadImportedTools()
        {
            var filePath = Path.Combine(_toolsStoragePath, "imported_tools.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var tools = JsonSerializer.Deserialize<List<ImportedTool>>(json);
                if (tools != null)
                {
                    _importedTools.AddRange(tools);
                    
                    // 尝试加载所有已导入的工具
                    foreach (var tool in _importedTools)
                    {
                        LoadCompiledTool(tool.NameSpace, tool.ClassName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载已导入工具信息失败: {Message}", ex.Message);
            }
        }
    }
} 