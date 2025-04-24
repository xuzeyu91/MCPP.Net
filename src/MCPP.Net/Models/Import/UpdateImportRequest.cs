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
    }
}
