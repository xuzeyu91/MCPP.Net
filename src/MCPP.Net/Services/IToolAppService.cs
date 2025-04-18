using MCPP.Net.Models;
using MCPP.Net.Repositories;
using ModelContextProtocol.Server;

namespace MCPP.Net.Services
{
    public interface IToolAppService
    {
        /// <summary>
        /// 添加工具
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        McpServerTool NewTool(ApiToolDto input);

        // 获取所有Tools
        List<McpServerTool> Tools();

        bool InsertTool(ApiToolDto input);

        bool DeleteTool(string name);

        tool GetToolByName(string name);

        List<tool> GetToolList();
    }
}
