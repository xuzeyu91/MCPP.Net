namespace MCPP.Net.Models.Import
{
    /// <summary>
    /// 导入成功后的响应
    /// </summary>
    public class ImportResponse
    {
        /// <summary>
        /// </summary>
        public ImportResponse(long id) : this(id, 0, []) { }

        /// <summary>
        /// </summary>
        public ImportResponse(long id, int successCount, List<Failure> faileds)
        {
            Id = id;
            SuccessCount = successCount;
            Faileds = faileds;
        }

        /// <summary>
        /// 导入成功后的 import id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 导入成功的 tools 数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 导入失败的详细信息
        /// </summary>
        public List<Failure> Faileds { get; set; }

        /// <summary>
        /// 导入失败的详细信息
        /// </summary>
        public record Failure(string HttpMethod, string RequestPath, string Reason);
    }
}
