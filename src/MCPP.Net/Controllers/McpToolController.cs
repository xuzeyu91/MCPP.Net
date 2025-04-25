using MCPP.Net.Models.Tool;
using MCPP.Net.Services;
using MCPP.Net.Services.Impl;
using Microsoft.AspNetCore.Mvc;

namespace MCPP.Net.Controllers
{
    /// <summary>
    /// Swagger导入API控制器
    /// </summary>
    [ApiController]
    [Route("api/mcptools")]
    public class McpToolController(IMcpToolService toolService) : ControllerBase
    {
        /// <summary>
        /// 单个导入 Tool
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Import([FromBody] CreateToolRequest request)
        {
            await toolService.ImportAsync(request);

            return Ok();
        }

        /// <summary>
        /// 查询所有 Tool
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] long importId)
        {
            var tools = await toolService.ListAsync(importId);

            return Ok(tools);
        }

        /// <summary>
        /// 查询单个 Tool
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var tool = await toolService.GetAsync(id);

            return Ok(tool);
        }

        /// <summary>
        /// 更新 Tool
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateToolRequest request)
        {
            await toolService.UpdateAsync(request);

            return Ok();
        }

        /// <summary>
        /// 删除 Tool
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await toolService.DeleteAsync(id);

            return Ok();
        }

        /// <summary>
        /// 根据 ImportId 删除全部关联的 Tool
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> Clear([FromQuery] long importId)
        {
            await toolService.DeleteByImportAsync(importId);

            return Ok();
        }

        /// <summary>
        /// 启用 import
        /// </summary>
        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(long id)
        {
            await toolService.SetEnabledAsync(id, true);

            return Ok();
        }

        /// <summary>
        /// 禁用 import
        /// </summary>
        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(long id)
        {
            await toolService.SetEnabledAsync(id, false);

            return Ok();
        }
    }
}
