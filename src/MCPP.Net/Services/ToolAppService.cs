using System.ComponentModel;
using System.Text;
using MCPP.Net.Models;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.Text.Json;
using MCPP.Net.Repositories;

namespace MCPP.Net.Services
{
    public class ToolAppService(
        IHttpClientFactory httpClientFactory,
        IToolRepositories toolRepo
        ) : IToolAppService
    {
        public McpServerTool NewTool(ApiToolDto input)
        {
            // 添加新工具
            var newTool = McpServerTool.Create(async ([Description("请求体 body")] JsonElement body) =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient();

                    var response = await client.PostAsync(
                        input.EndPoint,
                        new StringContent(body.ToString(), Encoding.UTF8, "application/json")
                    );

                    var responseContent = await response.Content.ReadAsStringAsync();

                    return responseContent;
                }
                catch (Exception ex)
                {

                    return ex.ToString();
                }
            });

            newTool.ProtocolTool.Name = input.Name;
            newTool.ProtocolTool.Description = input.Desc;

            if (!string.IsNullOrWhiteSpace(input.InputSchema))
            {
                // 更新InputSchema以匹配实际参数
                newTool.ProtocolTool.InputSchema = JsonSerializer.Deserialize<JsonElement>(input.InputSchema);
            }

            newTool.ProtocolTool.Annotations = new ToolAnnotations()
            {
                Title = input.Desc,
                OpenWorldHint = true
            };

            return newTool;
        }

        public List<McpServerTool> Tools()
        {
            var tools = GetToolList();
            var result = new List<McpServerTool>();

            foreach (var tool in tools)
            {
                var newTool = NewTool(tool.ToApiToolDto());
                result.Add(newTool);
            }

            return result;
        }

        public bool InsertTool(ApiToolDto input)
        {
            return toolRepo.Insert(input.ToTool());
        }

        public bool DeleteTool(string name)
        {
            return toolRepo.Delete(m => m.name == name);
        }

        public tool GetToolByName(string name)
        {
            return toolRepo.GetFirst(m => m.name == name);
        }

        public List<tool> GetToolList()
        {
            return toolRepo.GetList();
        }
    }
}
