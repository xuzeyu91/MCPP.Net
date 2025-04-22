using MCPP.Net.Database;
using MCPP.Net.Database.Entities;
using MCPP.Net.Models.Tool;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace MCPP.Net.Services.Impl
{
    internal class McpToolService(
        McppDbContext dbContext,
        ILogger<McpToolService> logger
        ) : IMcpToolService
    {
        internal static readonly string EmptyInputSchema;

        static McpToolService()
        {
            const string EMPTY_INPUT_SCHEMA = """
            {
                "type": "object",
                "properties": {
                    "parameters": {
                        "type": "object",
                        "properties": {
                        },
                        "required": []
                    }
                },
                "required": ["parameters"]
            }
            """;
            EmptyInputSchema = Regex.Replace(EMPTY_INPUT_SCHEMA, @"\s+", string.Empty);
        }

        public async Task ImportAsync(List<CreateToolRequest> requests)
        {
            var importIds = requests.Select(r => r.ImportId).Distinct().ToArray();
            if (importIds.Length > 1) throw new InvalidOperationException($"批量导入 Tool 时，不可一次导入不同 import 来源的数据，Import ids -> [{string.Join(',', importIds)}]");

            await DeleteByImportAsync(importIds[0]);

            await dbContext.McpTools.AddRangeAsync(requests.Select(x => x.ToTool()));
            var count = await dbContext.SaveChangesAsync();
            logger.LogInformation("批量导入 Tool({ImportId}) 时，新增了 {count} 条新数据", importIds[0], count);
        }

        public async Task ImportAsync(CreateToolRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                request.Name = ConvertPathToToolName(request.RequestPath);
            }
            if (string.IsNullOrEmpty(request.InputSchema))
            {
                request.InputSchema = EmptyInputSchema;
            }

            long? id = null;
            var enabled = true;
            if (request is UpdateToolRequest updateRequest)
            {
                id = updateRequest.Id;
                enabled = updateRequest.Enabled;
            }

            var tool = await FindAsync(request, id);

            if (tool is null)
            {
                tool = request.ToTool();
                dbContext.McpTools.Add(tool);
            }
            else
            {
                tool.Update(request, enabled);
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Tool 导入成功 -> {Id}, {Name}, {HttpMethod}, {RequestPath}", tool.Id, tool.Name, tool.HttpMethod, tool.RequestPath);
            logger.LogDebug("Tool 导入成功 [{Id}] -> {InputSchema}", tool.Id, tool.InputSchema);
        }

        /// <summary>
        /// 根据 ImportId 查询所有 Tool
        /// </summary>
        public async Task<List<QueryToolDto>> ListAsync(long importId)
        {
            return await dbContext.McpTools.Where(x => x.ImportId == importId).Select(x => x.ToDto()).ToListAsync();
        }

        /// <summary>
        /// 根据 Id 查询 Tool
        /// </summary>
        public async Task<QueryToolDto> GetAsync(long id)
        {
            var tool = await dbContext.McpTools.FindAsync(id) ?? throw new ArgumentException($"无法找到对应的 Tool({id})");
            
            return tool.ToDto();
        }

        public Task UpdateAsync(UpdateToolRequest request) => ImportAsync(request);

        public async Task DeleteAsync(long id)
        {
            var tool = await dbContext.McpTools.FindAsync(id) ?? throw new ArgumentException($"无法找到对应的 Tool({id})");
            dbContext.McpTools.Remove(tool);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Tool 删除成功 -> {Id}, {Name}, {HttpMethod}, {RequestPath}", tool.Id, tool.Name, tool.HttpMethod, tool.RequestPath);
        }

        /// <summary>
        /// 根据 ImportId 删除全部关联的 Tool
        /// </summary>
        public async Task DeleteByImportAsync(long importId)
        {
            var tools = dbContext.McpTools.Where(x => x.ImportId == importId);
            dbContext.McpTools.RemoveRange(tools);
            var count = await dbContext.SaveChangesAsync();
            logger.LogInformation("根据 ImportId({ImportId}) 删除了 {count} 条旧数据", importId, count);
        }

        public Task EnableAsync(long id) => UpdateEnabledAsync(id, true);

        public Task DisableAsync(long id) => UpdateEnabledAsync(id, false);

        private async Task UpdateEnabledAsync(long id, bool enabled)
        {
            var tool = await dbContext.McpTools.FindAsync(id) ?? throw new ArgumentException($"无法找到对应的 Tool({id})");
            tool.Enabled = enabled;
            await dbContext.SaveChangesAsync();
        }

        private async Task<McpTool?> FindAsync(CreateToolRequest request, long? id)
        {
            McpTool? tool = null;
            if (id.HasValue)
            {
                tool = await dbContext.McpTools.FirstOrDefaultAsync(x => x.Id == id);
                if (tool == null) throw new InvalidOperationException($"尝试更新一个不存在的 Tool(id: {id})");
            }

            if (tool is null)
            {
                tool = await dbContext.McpTools.FirstOrDefaultAsync(x => x.HttpMethod == request.HttpMethod && x.RequestPath == request.RequestPath);
            }
            else
            {
                var sameSignatureTool = await dbContext.McpTools.FirstOrDefaultAsync(x => x.HttpMethod == request.HttpMethod && x.RequestPath == request.RequestPath);
                if (sameSignatureTool != null)
                {
                    throw new InvalidOperationException($"已存在相同签名（HttpMethod, RequestPath）的 Tool({sameSignatureTool.Id}, {sameSignatureTool.Name}, {sameSignatureTool.HttpMethod}, {sameSignatureTool.RequestPath})");
                }
            }

            var sameNameTool = await dbContext.McpTools.FirstOrDefaultAsync(x => x.Name == request.Name);
            if (sameNameTool is not null && (tool is null || tool.Id != sameNameTool.Id))
            {
                throw new InvalidOperationException($"已存在相同名称的 Tool({sameNameTool.Id}, {sameNameTool.Name}, {sameNameTool.HttpMethod}, {sameNameTool.RequestPath})");
            }

            return tool;
        }

        /// <summary>
        /// 将路径转换为工具名称，将路径中的每一段使用`_`连接，对于路径参数使用`_{参数名}`替代
        /// 例如：/api/users/{userId} -> api_users__userId
        /// </summary>
        private static string ConvertPathToToolName(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("无法根据空的 Request Path 生成名称");

            var span = path.AsSpan();

            var builder = new StringBuilder();
            var segments = span.Split('/');
            foreach (var range in segments)
            {
                if (range.Start.Equals(range.End)) continue;

                builder.Append('_');
                var segment = span[range];
                if (segment.StartsWith('{'))
                {
                    segment = segment[1..^1];
                    builder.Append('_');
                }
                builder.Append(segment);
            }

            return builder.ToString(1, builder.Length - 1);
        }
    }
}
