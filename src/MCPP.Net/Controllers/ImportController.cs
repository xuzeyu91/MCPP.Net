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

        /// <summary>
        /// 导入Swagger API并动态注册为MCP工具
        /// </summary>
        /// <param name="request">导入请求参数(包含SwaggerUrl和可选的SourceBaseUrl)</param>
        /// <returns>导入结果</returns>
        [HttpPost]
        public async Task<IActionResult> Import([FromBody] SwaggerImportRequest request)
        {
            try
            {
                _logger.LogInformation("开始导入Swagger API: {Url}, 源服务器URL: {SourceUrl}", 
                    request.SwaggerUrl, 
                    string.IsNullOrEmpty(request.SourceBaseUrl) ? "(未提供)" : request.SourceBaseUrl);
               
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
        
        /// <summary>
        /// 删除已导入的API工具
        /// </summary>
        /// <param name="nameSpace">工具命名空间</param>
        /// <param name="className">类名</param>
        /// <returns>操作结果</returns>
        [HttpDelete]
        public IActionResult DeleteImportedTool(string nameSpace, string className)
        {
            try
            {
                bool result = _importService.DeleteImportedTool(nameSpace, className);
                if (result)
                {
                    return Ok(new { success = true, message = $"已删除工具: {nameSpace}.{className}" });
                }
                else
                {
                    return NotFound(new { success = false, message = $"未找到工具: {nameSpace}.{className}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除已导入工具失败: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 