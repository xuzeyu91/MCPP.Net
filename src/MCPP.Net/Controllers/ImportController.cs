using MCPP.Net.Models.Import;
using MCPP.Net.Services;
using Microsoft.AspNetCore.Mvc;

namespace MCPP.Net.Controllers
{
    /// <summary>
    /// Swagger导入API控制器
    /// </summary>
    [ApiController]
    [Route("api/imports")]
    public class ImportController(IImportService importService) : ControllerBase
    {
        /// <summary>
        /// swagger 导入
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Import([FromBody] CreateImportRequest request)
        {
            var response = await importService.ImportAsync(request);

            return Ok(response);
        }

        /// <summary>
        /// 查询所有 import.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var datas = await importService.ListAsync();

            return Ok(datas);
        }

        /// <summary>
        /// 查询单个 import
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var data = await importService.GetAsync(id);

            return Ok(data);
        }

        /// <summary>
        /// 更新 import
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateImportRequest request)
        {
            await importService.UpdateAsync(request);

            return Ok();
        }

        /// <summary>
        /// 删除 import
        /// </summary>

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await importService.DeleteAsync(id);

            return Ok();
        }

        /// <summary>
        /// 启用 import
        /// </summary>
        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(long id)
        {
            await importService.SetEnabledAsync(id, true);

            return Ok();
        }

        /// <summary>
        /// 禁用 import
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(long id)
        {
            await importService.SetEnabledAsync(id, false);

            return Ok();
        }

        /// <summary>
        /// 重新导入
        /// </summary>
        [HttpPost("{id}/reimport")]
        public async Task<IActionResult> Reimport(long id)
        {
            await importService.ReimportAsync(id);

            return Ok();
        }
    }
} 