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
        private readonly List<ImportedTool> _importedTools = new List<ImportedTool>();

        public ImportedToolsService(
            ILogger<ImportedToolsService> logger,
            IMcpServerMethodRegistry methodRegistry,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _methodRegistry = methodRegistry;
            _toolsStoragePath = Path.Combine(hostEnvironment.ContentRootPath, "ImportedTools");
            
            // 创建导入工具存储目录
            if (!Directory.Exists(_toolsStoragePath))
            {
                Directory.CreateDirectory(_toolsStoragePath);
            }
            
            // 加载已导入的工具信息
            LoadImportedTools();
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