using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MCPP.Net.Models.Tool
{
    [DebuggerDisplay("[{Name}] {HttpMethod} -> {RequestPath}")]
    public class CreateToolRequest
    {
        /// <summary>
        /// import表关联ID
        /// </summary>
        [Required]
        public long ImportId { get; set; }

        /// <summary>
        /// http method
        /// </summary>
        [Required]
        public required string HttpMethod { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [Required]
        public required string RequestPath { get; set; }

        /// <summary>
        /// 与 import 表的 name 拼接后为 MCP Tool Name，默认根据 request_path 生成，可修改
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 导入时从 swagger 中提取，可修改，方便 MCP Client 匹配
        /// </summary>
        [Required]
        public required string Description { get; set; }

        /// <summary>
        /// 导入时通过 swagger 信息生成
        /// </summary>
        [Required]
        public required string InputSchema { get; set; }
    }
}
