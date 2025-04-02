using System.ComponentModel.DataAnnotations;

namespace MCPP.Net.Models
{
    /// <summary>
    /// Swagger导入请求
    /// </summary>
    public class SwaggerImportRequest
    {
        /// <summary>
        /// Swagger文档URL或本地路径
        /// </summary>
        [Required]
        public string SwaggerUrl { get; set; } = string.Empty;

        /// <summary>
        /// 生成类的命名空间
        /// </summary>
        [Required]
        public string NameSpace { get; set; } = "MCPP.Net.DynamicTools";

        /// <summary>
        /// 生成的类名
        /// </summary>
        [Required]
        public string ClassName { get; set; } = "DynamicApiTool";
    }

    /// <summary>
    /// Swagger导入结果
    /// </summary>
    public class SwaggerImportResult
    {
        /// <summary>
        /// 导入是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 导入的API数量
        /// </summary>
        public int ApiCount { get; set; }

        /// <summary>
        /// 工具类名称
        /// </summary>
        public string ToolClassName { get; set; } = string.Empty;

        /// <summary>
        /// 导入的API名称列表
        /// </summary>
        public List<string> ImportedApis { get; set; } = new List<string>();
    }

    /// <summary>
    /// 已导入的API工具信息
    /// </summary>
    public class ImportedTool
    {
        /// <summary>
        /// 工具命名空间
        /// </summary>
        public string NameSpace { get; set; } = string.Empty;

        /// <summary>
        /// 工具类名
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// API数量
        /// </summary>
        public int ApiCount { get; set; }

        /// <summary>
        /// 导入日期
        /// </summary>
        public DateTime ImportDate { get; set; }

        /// <summary>
        /// Swagger来源
        /// </summary>
        public string SwaggerSource { get; set; } = string.Empty;
    }
} 