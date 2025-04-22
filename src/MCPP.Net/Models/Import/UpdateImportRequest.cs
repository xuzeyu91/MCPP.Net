using System.ComponentModel.DataAnnotations;

namespace MCPP.Net.Models.Import
{
    /// <summary>
    /// </summary>
    public class UpdateImportRequest : CreateImportRequest
    {
        /// <summary>
        /// 自增ID
        /// </summary>
        [Required]
        public required long Id { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public required bool Enabled { get; set; }
    }
}
