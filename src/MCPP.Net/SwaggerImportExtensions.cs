using ModelContextProtocol.Server;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCPP.Net
{
    /// <summary>
    /// MCP Server Builder的Swagger导入扩展
    /// </summary>
    public static class SwaggerImportExtensions
    {
        private const string WithToolsRequiresUnreferencedCodeMessage = "反射获取类型需要禁用代码裁剪.";

        /// <summary>
        /// 添加动态生成的Swagger工具类型
        /// </summary>
        /// <param name="builder">MCP服务器构建器</param>
        /// <param name="dynamicToolTypes">动态生成的工具类型列表</param>
        /// <returns>MCP服务器构建器</returns>
        public static IMcpServerBuilder WithSwaggerTools(this IMcpServerBuilder builder, IEnumerable<Type> dynamicToolTypes)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (dynamicToolTypes == null)
            {
                return builder;
            }

            // 过滤已标记McpServerToolTypeAttribute的类型
            var toolTypes = dynamicToolTypes
                .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null);

            return builder.WithTools(toolTypes);
        }
    }
} 