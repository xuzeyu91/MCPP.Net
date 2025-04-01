using MCPP.Net.Models;
using MCPP.Net.Services;
using Microsoft.AspNetCore.Mvc;

namespace MCPP.Net.Controllers
{
    /// <summary>
    /// Swagger导入API控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ImportController : ControllerBase
    {
        private readonly ILogger<ImportController> _logger;
        private readonly SwaggerImportService _importService;

        public ImportController(
            ILogger<ImportController> logger,
            SwaggerImportService importService)
        {
            _logger = logger;
            _importService = importService;
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            return Ok("hello world");
        }
        /// <summary>
        /// 导入Swagger API并动态注册为MCP工具
        /// </summary>
        /// <param name="request">导入请求参数</param>
        /// <returns>导入结果</returns>
        [HttpPost]
        public async Task<IActionResult> Import([FromBody] SwaggerImportRequest request)
        {
            try
            {
                _logger.LogInformation("开始导入Swagger API: {Url}", request.SwaggerUrl);
                
                var result = await _importService.ImportSwaggerAsync(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入Swagger API失败: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取当前已导入的所有API工具
        /// </summary>
        /// <returns>已导入的API工具列表</returns>
        [HttpGet]
        public IActionResult GetImportedTools()
        {
            try
            {
                var tools = _importService.GetImportedTools();
                return Ok(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取已导入工具失败: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 