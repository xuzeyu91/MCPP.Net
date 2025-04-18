using MCPP.Net.Common.Model;
using MCPP.Net.Models;
using MCPP.Net.Repositories;
using MCPP.Net.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace MCPP.Net.Controllers
{
    /// <summary>
    /// 手动添加工具
    /// </summary>
    /// <param name="mcpServerOptions"></param>
    /// <param name="httpClientFactory"></param>
    [ApiController]
    [Route("[controller]")]
    public class ToolController(
        IOptions<McpServerOptions> mcpServerOptions,
        IToolAppService toolAppService,
        IToolRepositories toolRepo
        ) : ControllerBase
    {
        //添加
        [HttpPost]
        public Result Add([FromBody] ApiToolDto input)
        {
            McpServerOptions options = mcpServerOptions.Value;

            var serverTools = options.Capabilities?.Tools?.ToolCollection;

            if (serverTools is null)
            {
                options.Capabilities ??= new();
                options.Capabilities.Tools ??= new();
                options.Capabilities.Tools.ToolCollection = [toolAppService.NewTool(input)];
                options.Capabilities.Tools.ListChanged = true;

                toolAppService.InsertTool(input);
                //返回错误
                return "添加成功".Success();
            }

            // 检查是否已经存在
            var existingTool = serverTools.FirstOrDefault(t => t.ProtocolTool.Name == input.Name);
            if (existingTool != null)
            {
                return "工具已存在".Success();
            }

            var newTool = toolAppService.NewTool(input);

            serverTools.Add(newTool);

            toolAppService.InsertTool(input);
            return "添加成功".Success();
        }

        //删除
        [HttpDelete("{name}")]
        public Result Delete(string name)
        {
            McpServerOptions options = mcpServerOptions.Value;
            var serverTools = options.Capabilities?.Tools?.ToolCollection;

            if (serverTools is null || !serverTools.Any())
            {
                return "".Error("500", "没有找到任何工具");
            }

            var existingTool = serverTools.FirstOrDefault(t => t.ProtocolTool.Name == name);
            if (existingTool == null)
            {

                return "".Error("500", $"未找到名称为 '{name}' 的工具");
            }

            serverTools.Remove(existingTool);

            toolAppService.DeleteTool(name);
            return $"工具 '{name}' 删除成功"
            .Success();
        }

        //更新
        [HttpPut("{name}")]
        public Result Update(string name, [FromBody] ApiToolDto input)
        {
            //删除   
            Delete(name);

            //添加
            Add(input);

            return ($"工具 '{name}' 更新成功").Success();
        }

        //获取
        [HttpGet("{name}")]
        public Result Get(string name)
        {
            var tool = toolAppService.GetToolByName(name);

            if (tool == null)
            {
                return "".Error("500", ($"未找到名称为 '{name}' 的工具"));
            }

            return tool.Success();
        }

        //获取所有
        [HttpGet]
        public Result GetAll()
        {
            var tools = toolAppService.GetToolList();

            if (tools == null || !tools.Any())
            {
                return "".Error("500", "没有找到任何工具");
            }

            return tools.Success();
        }

    }
}
