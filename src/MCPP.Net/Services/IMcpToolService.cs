using MCPP.Net.Models.Tool;

namespace MCPP.Net.Services
{
    /// <summary>
    /// MCP Tool Service Interface
    /// </summary>
    public interface IMcpToolService
    {
        /// <summary>
        /// 批量导入，覆盖所有 ImportId 相同的 Tool
        /// </summary>
        Task ImportAsync(List<CreateToolRequest> requests);

        /// <summary>
        /// 单个导入，逻辑类似 Insert or Update，根据 HttpMethod 和 RequestPath 进行匹配已存在的记录
        /// </summary>
        Task ImportAsync(CreateToolRequest request);

        /// <summary>
        /// 根据 ImportId 查询所有 Tool
        /// </summary>
        Task<List<QueryToolDto>> ListAsync(long importId);

        /// <summary>
        /// 根据 Id 查询 Tool
        /// </summary>
        Task<QueryToolDto> GetAsync(long id);

        /// <summary>
        /// 根据 Id 更新 Tool
        /// </summary>
        Task UpdateAsync(UpdateToolRequest request);

        /// <summary>
        /// 删除 Tool
        /// </summary>
        Task DeleteAsync(long id);

        /// <summary>
        /// 根据 ImportId 删除全部关联的 Tool
        /// </summary>
        Task DeleteByImportAsync(long importId);
    }
}
