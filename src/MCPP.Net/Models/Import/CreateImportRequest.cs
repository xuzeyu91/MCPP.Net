using System.ComponentModel.DataAnnotations;

namespace MCPP.Net.Models.Import
{
    /// <summary>
    /// </summary>
    public class CreateImportRequest
    {
        /// <summary>
        /// 导入时设定的名称，长度限制[1,5]
        /// </summary>
        [Required]
        [MaxLength(5)]
        [MinLength(1)]
        public required string Name { get; set; }

        /// <summary>
        /// 导入链接
        /// </summary>
        public string? ImportFrom { get; set; }

        /// <summary>
        /// 源服务器基础URL
        /// </summary>
        [Required]
        public required string SourceBaseUrl { get; set; }

        /// <summary>
        /// 对本条记录的描述，简短的 name 可能词不达意，用描述进行补充说明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// swagger.json
        /// </summary>
        public string? Json { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public required bool Enabled { get; set; }
    }
}
