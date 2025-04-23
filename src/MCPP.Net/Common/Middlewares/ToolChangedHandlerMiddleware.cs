using MCPP.Net.Core;
using MCPP.Net.Database;

namespace MCPP.Net.Common.Middlewares
{
    /// <summary>
    /// 用于检查每次请求是否有 MCP Tool 的更改，并执行相应的操作
    /// </summary>
    internal class ToolChangedHandlerMiddleware(RequestDelegate next, McpToolsKeeper toolsKeeper)
    {
        public async Task InvokeAsync(HttpContext context, McppDbContext dbContext)
        {
            try
            {
                await next(context);
            }
            finally
            {
                if (dbContext.HasChanged)
                {
                    toolsKeeper.NotifyDataChanged();
                }
            }
        }
    }
}
