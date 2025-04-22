using MCPP.Net.Models.Import;

namespace MCPP.Net.Services
{
    /// <summary>
    /// MCP Import Service Interface    
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// 导入 import.
        /// 支持从 <see cref="CreateImportRequest.ImportFrom"/> 中下载 swagge.json；
        /// 支持通过 <see cref="CreateImportRequest.Json"/> 直接导入；
        /// 支持两个参数都不传入，此时该 import 为手动管理的 import
        /// </summary>
        Task ImportAsync(CreateImportRequest request);

        /// <summary>
        /// 查询所有 import.
        /// </summary>
        Task<List<QueryImportDto>> ListAsync();

        /// <summary>
        /// 查询单个 import
        /// </summary>
        Task<QueryImportDto> GetAsync(long id);

        /// <summary>
        /// 更新 import
        /// </summary>
        Task UpdateAsync(UpdateImportRequest request);

        /// <summary>
        /// 删除 import
        /// </summary>
        Task DeleteAsync(long id);
    }
}
