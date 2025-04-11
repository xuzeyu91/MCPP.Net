using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace MCPP.Net.Core
{
    /// <summary>
    /// 覆盖 <see cref="McpServerOptions"/> 默认配置，使用 <see cref="McpToolsKeeper"/> 管理 Tools，方便后续的增删操作
    /// </summary>
    public class McpServerOptionsPostConfigure(McpToolsKeeper toolsKeeper) : IPostConfigureOptions<McpServerOptions>
    {
        /// <inheritdoc />
        public void PostConfigure(string? name, McpServerOptions options)
        {
            if (options.Capabilities?.Tools?.ToolCollection == null) throw new ArgumentNullException("MCP tools collection is null");

            toolsKeeper.SetTools(options.Capabilities.Tools);

            options.Capabilities.Tools.ToolCollection = [];
            options.Capabilities.Tools.ListToolsHandler = toolsKeeper.ListToolsHandler;
            options.Capabilities.Tools.CallToolHandler = toolsKeeper.CallToolHandler;
        }
    }
}
