using MCPP.Net.Models;
using MCPP.Net.UnsafeImports;
using ModelContextProtocol.Server;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace MCPP.Net.Core
{
    /// <summary>
    /// <see cref="IToolAssemblyLoader"/> 的默认实现
    /// </summary>
    public class ToolAssemblyLoader(McpToolsKeeper toolsKeeper, ILogger<ToolAssemblyLoader> logger) : IToolAssemblyLoader
    {
        private readonly ConcurrentDictionary<string, PluginAssembly> _assemblies = new();

        /// <inheritdoc/>
        public ToolLoadedDetail Load(string assemblyPath)
        {
            // 创建自定义上下文加载程序集，避免重复加载同名程序集
            var loadContextName = Guid.NewGuid().ToString();
            var loadContext = new AssemblyLoadContext(loadContextName, true);

            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

                var assemblyName = assembly.GetName().Name!;

                var pluginAssembly = new PluginAssembly(assembly, loadContext);

                var importedTools = LoadToolsFromAssembly(pluginAssembly);

                _assemblies[assemblyName] = pluginAssembly;

                return importedTools;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "加载动态程序集失败: {Path}", assemblyPath);
                throw;
            }
        }

        /// <inheritdoc/>
        public void Unload(string assemblyName)
        {
            if (UnloadInternal(assemblyName))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private bool UnloadInternal(string assemblyName)
        {
            if (_assemblies.TryRemove(assemblyName, out var pluginAssembly))
            {
                toolsKeeper.Remove(assemblyName);
                pluginAssembly.Dispose();

                return true;
            }
            return false;
        }

        private ToolLoadedDetail LoadToolsFromAssembly(PluginAssembly pluginAssembly)
        {
            var importedTools = new List<ImportedTool>();
            var tools = new List<McpServerTool>();
            var registeredMethods = new List<string>();

            foreach (var type in pluginAssembly.Assembly.GetTypes())
            {
                // 查找带有McpServerToolType特性的类型
                var toolTypeAttr = type.GetCustomAttribute<McpServerToolTypeAttribute>();
                if (toolTypeAttr != null)
                {
                    logger.LogInformation("找到工具类型: {TypeName}", type.FullName);

                    // 获取所有带有McpServerTool特性的方法
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
                        .ToList();

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

                    if (methods.Count == 0) continue;

                    tools.AddRange(methods.Select(x => UnsafeAIFunctionMcpServerTool.Create(x, pluginAssembly.SerializerOptions!)));
                    importedTools.Add(importedTool);
                    registeredMethods.AddRange(methods.Select(x => x.Name));

                    logger.LogInformation("将注册 {Count} 个方法, 来自 {TypeName}", methods.Count, type.FullName);
                }
            }

            var assemblyName = pluginAssembly.Assembly.GetName().Name!;
            toolsKeeper.Add(assemblyName, tools);

            logger.LogInformation("已从 {AssemblyName} 中加载 {ToolCount} 个工具", assemblyName, tools.Count);

            return new(registeredMethods, importedTools);
        }
    }
}
