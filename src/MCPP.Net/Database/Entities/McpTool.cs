using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MCPP.Net.Database.Entities
{
    /// <summary>
    /// MCP Tool 信息表，一条记录对应一个 MCP Tool，与导入表关联
    /// </summary>
    [Table("mcp_tool")]
    [Index(nameof(HttpMethod), nameof(RequestPath), IsUnique = true, Name = "IX_McpTool_HttpMethod_RequestPath")]
    [Index(nameof(Name), IsUnique = true, Name = "IX_McpTool_Name")]
    public class McpTool
    {
        /// <summary>
        /// 自增ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// import表关联ID
        /// </summary>
        [Column("import_id")]
        [Required]
        public long ImportId { get; set; }
        
        /// <summary>
        /// 导入表关联
        /// </summary>
        [ForeignKey("ImportId")]
        public virtual Import Import { get; set; } = null!;

        /// <summary>
        /// http method
        /// </summary>
        [Column("http_method")]
        [Required]
        public required string HttpMethod { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [Column("request_path")]
        [Required]
        public required string RequestPath { get; set; }

        /// <summary>
        /// 与 import 表的 name 拼接后为 MCP Tool Name，默认根据 request_path 生成，可修改
        /// </summary>
        [Column("name")]
        [Required]
        public required string Name { get; set; }

        /// <summary>
        /// 导入时从 swagger 中提取，可修改，方便 MCP Client 匹配
        /// </summary>
        [Column("description")]
        [Required]
        public required string Description { get; set; }

        /// <summary>
        /// 导入时通过 swagger 信息生成
        /// </summary>
        [Column("input_schema")]
        [Required]
        public required string InputSchema { get; set; }

        /// <summary>
        /// 是否启用，可修改，方便筛选掉不需要的接口
        /// </summary>
        [Column("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
