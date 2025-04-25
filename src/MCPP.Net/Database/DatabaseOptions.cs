namespace MCPP.Net.Database
{
    /// <summary>
    /// 数据库配置选项
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>
        /// 数据库类型，目前支持 sqlite 和 postgresql
        /// </summary>
        public required string DbType { get; set; }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public required string ConnectionString { get; set; }
    }
}
