using Microsoft.OpenApi.Models;
using MCPP.Net.Models;
using System.Collections.Concurrent;

namespace MCPP.Net.Services
{
    /// <summary>
    /// Swagger导入服务的扩展方法
    /// </summary>
    public static class SwaggerImportExtensions
    {
        // 存储导入的工具信息
        private static readonly ConcurrentBag<ImportedTool> _importedTools = new ConcurrentBag<ImportedTool>();

        /// <summary>
        /// 导入Swagger API并注册为MCP工具
        /// </summary>
        /// <param name="service">Swagger导入服务</param>
        /// <param name="swaggerUrl">Swagger文档URL或本地路径</param>
        /// <param name="nameSpace">生成类的命名空间</param>
        /// <param name="className">生成的类名</param>
        /// <returns>导入结果</returns>
        public static async Task<SwaggerImportResult> ImportAndRegisterToolsAsync(
            this SwaggerImportService service,
            string swaggerUrl,
            string nameSpace,
            string className)
        {
            var result = new SwaggerImportResult
            {
                Success = false,
                ToolClassName = className,
                ImportedApis = new List<string>()
            };

            try
            {
                // 导入Swagger API
                int importedCount = await service.ImportFromUrlAsync(swaggerUrl);

                // 记录导入的API信息
                var tool = new ImportedTool
                {
                    NameSpace = nameSpace,
                    ClassName = className,
                    ApiCount = importedCount,
                    ImportDate = DateTime.UtcNow,
                    SwaggerSource = swaggerUrl
                };

                // 添加到已导入工具列表
                _importedTools.Add(tool);

                // 设置结果
                result.Success = true;
                result.ApiCount = importedCount;
                
                return result;
            }
            catch (Exception ex)
            {
                // 记录日志由调用方处理
                throw;
            }
        }

        /// <summary>
        /// 获取所有已导入的API工具
        /// </summary>
        /// <param name="service">Swagger导入服务</param>
        /// <returns>已导入的工具列表</returns>
        public static List<ImportedTool> GetImportedTools(this SwaggerImportService service)
        {
            return _importedTools.ToList();
        }
    }
} 