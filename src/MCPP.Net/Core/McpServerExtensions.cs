using MCPP.Net.Services;
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

        public static IMcpServerBuilder WithDBTools(this IMcpServerBuilder builder, IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var toolAppService = scope.ServiceProvider.GetRequiredService<IToolAppService>();

            var tools = toolAppService.Tools();

            foreach (var tool in tools)
            {
                builder.Services.AddSingleton(tool);
            }

            return builder;
        }
    }
}
