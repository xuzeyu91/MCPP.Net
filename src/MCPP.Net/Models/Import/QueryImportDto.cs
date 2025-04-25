namespace MCPP.Net.Models.Import
{
    /// <summary>
    /// </summary>
    public sealed class QueryImportDto : UpdateImportRequest
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
