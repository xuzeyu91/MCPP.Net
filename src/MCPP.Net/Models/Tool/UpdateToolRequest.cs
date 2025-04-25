using System.ComponentModel.DataAnnotations.Schema;

namespace MCPP.Net.Models.Tool
{
    public class UpdateToolRequest : CreateToolRequest
    {
        /// <summary>
        /// 自增ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Column("enabled")]
        public bool Enabled { get; set; }
    }
}
