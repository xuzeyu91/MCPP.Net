using MCPP.Net.Models.Import;

namespace MCPP.Net.Services
{
    /// <summary>
    /// MCP Import Service Interface    
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// 相当于 Insert 方法，但如果导入时发现数据库已存在相同 Name 和 ImportFrom 的记录，会直接覆盖，
        /// 如果 Name 相同但 ImportFrom 不同，会拒绝本次操作并提示 Name 已存在。
        /// 如果需要更新某条记录的 ImportFrom，请调用 <see cref="UpdateAsync(UpdateImportRequest)"/>
        /// </summary>
        Task<ImportResponse> ImportAsync(CreateImportRequest request);

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

        /// <summary>
        /// 启用/禁用 import
        /// </summary>
        Task SetEnabledAsync(long id, bool enabled);

        /// <summary>
        /// 强制重新导入，可以用于应对导入失败的情况，也可以用于下载最新的 swagger.json 进行更新
        /// </summary>
        Task ReimportAsync(long id);
    }
}
