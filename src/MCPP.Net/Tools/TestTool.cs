using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCPP.Net.Tools
{
    [McpServerToolType]
    public static class TestTool
    {
        [McpServerTool(Name = "test"), Description("这是一个测试函数，用于测试MCP Tool")]
        public static string Test([Description("测试消息")] string message)
        {
            return "hello " + message;
        }
    }
}
