using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace MCPP.Net.Core
{
    /// <summary>
    /// <see cref="IMcpServerBuilder"/> extension methods
    /// </summary>
    public static class McpServerExtensions
    {
        /// <summary>
        /// 使用 <see cref="McpToolsKeeper"/> 管理 Tools，方便后续的增删操作
        /// </summary>
        public static IMcpServerBuilder UseToolsKeeper(this IMcpServerBuilder builder)
        {
            builder.Services.AddSingleton<McpToolsKeeper>();
            builder.Services.AddTransient<IPostConfigureOptions<McpServerOptions>, McpServerOptionsPostConfigure>();

            return builder;
        }
    }
}
