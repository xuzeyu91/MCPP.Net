using Microsoft.Extensions.Options;
using System.Reflection;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// MCP服务器方法注册实现
    /// </summary>
    public class McpServerMethodRegistry : IMcpServerMethodRegistry
    {
        private readonly ILogger<McpServerMethodRegistry> _logger;
        private readonly List<MethodInfo> _registeredMethods = new List<MethodInfo>();
        private readonly McpServerOptions _mcpServerOptions;

        public McpServerMethodRegistry(ILogger<McpServerMethodRegistry> logger, IOptions<McpServerOptions> mcpServerOptions)
        {
            _logger = logger;
            _mcpServerOptions = mcpServerOptions.Value;
        }

        /// <summary>
        /// 添加方法到MCP服务器
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        public void AddMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }
            _registeredMethods.Add(methodInfo);

            //动态添加Tool到MCP服务器
            var serverTools = _mcpServerOptions.Capabilities?.Tools?.ToolCollection;
            serverTools?.Add(McpServerTool.Create(methodInfo));

            _logger.LogInformation("已注册方法: {MethodName}", methodInfo.Name);
        }

        public void Clear()
        {
            var serverTools = _mcpServerOptions.Capabilities?.Tools?.ToolCollection;
            serverTools?.Clear();
        }

        /// <summary>
        /// 获取已注册的所有方法
        /// </summary>
        /// <returns>已注册的方法列表</returns>
        public IReadOnlyList<MethodInfo> GetRegisteredMethods()
        {
            return _registeredMethods.AsReadOnly();
        }
    }
}