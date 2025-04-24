using MCPP.Net.Database;
using MCPP.Net.Database.Entities;
using MCPP.Net.Models.Tool;
using Microsoft.EntityFrameworkCore;

namespace MCPP.Net.Services.Impl
{
    internal class McpToolService(
        McppDbContext dbContext,
        ILogger<McpToolService> logger
        ) : IMcpToolService
    {
        public async Task ImportAsync(List<CreateToolRequest> requests)
        {
            var importIds = requests.Select(r => r.ImportId).Distinct().ToArray();
            if (importIds.Length > 1) throw new InvalidOperationException($"批量导入 Tool 时，不可一次导入不同 import 来源的数据，Import ids -> [{string.Join(',', importIds)}]");

            await DeleteByImportAsync(importIds[0]);

            var tools = requests.Select(x => x.ToTool());
            await dbContext.McpTools.AddRangeAsync(tools);
            var count = await dbContext.SaveChangesAsync();
            logger.LogInformation("批量导入 Tool({ImportId}) 时，新增了 {count} 条新数据", importIds[0], count);
        }

        public async Task ImportAsync(CreateToolRequest request)
        {
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

        public async Task SetEnabledAsync(long id, bool enabled)
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
    }
}
