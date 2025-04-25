using System.Text.Json;

namespace MCPP.Net.Common.Middlewares
{
    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    internal class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "请求处理过程中发生异常: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = JsonSerializer.Serialize(new { error = exception.Message });
            await context.Response.WriteAsync(response);
        }
    }
} 