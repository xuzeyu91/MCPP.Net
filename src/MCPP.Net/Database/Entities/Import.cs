using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MCPP.Net.Database.Entities
{
    /// <summary>
    /// 导入表，一条记录对应一个 swagger.json
    /// </summary>
    [Table("import")]
    [Index(nameof(ImportFrom), IsUnique = true, Name = "IX_Import_ImportFrom")]
    [Index(nameof(Name), IsUnique = true, Name = "IX_Import_Name")]
    public class Import
    {
        /// <summary>
        /// 自增ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// 导入时设定的名称，长度限制[1,5]
        /// </summary>
        [Column("name")]
        [Required]
        [MaxLength(5)]
        [MinLength(1)]
        public required string Name { get; set; }

        /// <summary>
        /// 导入链接
        /// </summary>
        [Column("import_from")]
        public string? ImportFrom { get; set; }

        /// <summary>
        /// 源服务器基础URL
        /// </summary>
        [Required]
        [Column("source_base_url")]
        public required string SourceBaseUrl { get; set; }

        /// <summary>
        /// 对本条记录的描述，简短的 name 可能词不达意，用描述进行补充说明
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// swagger.json
        /// </summary>
        [Column("json")]
        public string? Json { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Column("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// 关联的所有MCP工具
        /// </summary>
        public virtual ICollection<McpTool> McpTools { get; set; } = [];

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
