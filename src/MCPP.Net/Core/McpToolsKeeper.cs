using MCPP.Net.Database;
using MCPP.Net.Database.Entities;
using MCPP.Net.UnsafeImports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Text;
using Azure.Core;

namespace MCPP.Net.Core
{
    /// <summary>
    /// Tool 管理员，可以在运行时添加和删除 Tools
    /// </summary>
    public class McpToolsKeeper(ILogger<McpToolsKeeper> logger)
    {
        private static readonly object MAP_KEY = new();

        private readonly AIFunction _forwardCall = AIFunctionFactory.Create(ForwardCallAsync);
        private ToolsCapability _tools = null!;

        /// <summary>
        /// 使用原始配置初始化<see cref="McpToolsKeeper"/>
        /// </summary>
        public void SetTools(ToolsCapability tools) => _tools = tools;

        /// <summary>
        /// 添加一个新的 Tool 集合
        /// </summary>
        public void Add(string id, IEnumerable<McpServerTool> tools) { }

        /// <summary>
        /// 移除一个 Tool 集合
        /// </summary>
        public void Remove(string id) { }

        /// <summary>
        /// 获取所有 Tool 集合
        /// </summary>
        public async ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams> context, CancellationToken token)
        {
            var result = new ListToolsResult();

            if (_tools.ToolCollection is { } collection)
            {
                result.Tools.AddRange(collection.Select(x => x.ProtocolTool));
            }

            var dbContext = context.Services!.GetRequiredService<McppDbContext>();

            var imports = await dbContext.Imports.Where(x => x.Enabled).ToArrayAsync(token);
            var importTools = imports.Select(x => x.McpTools.Select(y => T2t(y))).SelectMany(x => x);

            result.Tools.AddRange(importTools);

            return result;
        }

        /// <summary>
        /// 调用指定的 Tool
        /// </summary>
        public async ValueTask<CallToolResponse> CallToolHandler(RequestContext<CallToolRequestParams> context, CancellationToken token)
        {
            if (context.Params is not { } param) throw new InvalidOperationException($"无法获取 Tool name");

            var toolName = param.Name;
            if (_tools.ToolCollection is { } tools && tools.TryGetPrimitive(toolName, out var tool))
            {
                return await tool.InvokeAsync(context, token);
            }

            var dbContext = context.Services!.GetRequiredService<McppDbContext>();

            var mcpTool = await dbContext.McpTools.FirstOrDefaultAsync(x => x.Import.Name + "_" + x.Name == toolName);

            if (mcpTool is null) throw new InvalidOperationException($"无法找到名为 {toolName} 的 Mcp Server Tool");

            var arguments = new AIFunctionArguments
            {
                Services = context.Services,
                Context = new Dictionary<object, object?>
                {
                    [typeof(RequestContext<CallToolRequestParams>)] = context,
                    [MAP_KEY] = mcpTool
                }
            };
            if (context.Params?.Arguments is { } args)
            {
                foreach (var kvp in args)
                {
                    arguments[kvp.Key] = kvp.Value;
                }
            }

            var resultObj = await _forwardCall.InvokeAsync(arguments, token);

            var content = new Content
            {
                Type = "text",
                Text = resultObj switch
                {
                    string str => str,
                    JsonElement json => json.ToString(),
                    _ => JsonSerializer.Serialize(resultObj)
                }
            };

            return new() { Content = [content] };
        }

        /// <summary>
        /// 通知数据库中的数据发生了变化
        /// </summary>
        public void NotifyDataChanged()
        {
            logger.LogInformation("数据库中的数据发生了变化，通知 MCP 客户端 Tool list 发生了变动");
            UnsafeMcpServerPrimitiveCollection<McpServerTool>.RaiseChanged(_tools!.ToolCollection!);
        }

        private static Tool T2t(McpTool tool)
        {
            return new Tool
            {
                Name = $"{tool.Import.Name}_{tool.Name}",
                Description = tool.Description,
                InputSchema = JsonDocument.Parse(tool.InputSchema).RootElement,
                Annotations = new() { OpenWorldHint = true }
            };
        }

        private static async Task<string> ForwardCallAsync(AIFunctionArguments arguments, JsonElement? parameters = null)
        {
            if (arguments.Context is not { } context || !context.TryGetValue(MAP_KEY, out var toolObj) || toolObj is not McpTool tool)
            {
                throw new InvalidOperationException($"无法从 arguments 中获取 McpTool");
            }

            var httpClientFactory = arguments.Services!.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            var message = HttpRequestBuilder.Build(httpClient, tool, parameters);

            var response = await httpClient.SendAsync(message);
            var content = await response.Content.ReadAsStringAsync();

            return content;
        }

        private class HttpRequestBuilder
        {
            public static HttpRequestMessage Build(HttpClient httpClient, McpTool tool, JsonElement? parameters)
            {
                var request = new HttpRequestMessage();

                request.Method = new HttpMethod(tool.HttpMethod);

                var requestPath = tool.RequestPath;

                requestPath = ProcessPath(requestPath, parameters);

                var queryParams = ProcessQuery(parameters);

                request.RequestUri = new Uri(BuildUrl(requestPath, queryParams, tool));

                ProcessHeader(request, parameters);

                ProcessJson(request, parameters);

                ProcessForm(request, parameters);

                return request;
            }

            private static string BuildUrl(string requestPath, List<string>? queryParameters, McpTool tool)
            {
                var baseUrl = tool.Import.SourceBaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/{requestPath.TrimStart('/')}";
                if (queryParameters?.Count > 0)
                {
                    url += $"?{string.Join("&", queryParameters)}";
                }

                return url;
            }

            private static string ProcessPath(string requestPath, JsonElement? parameters)
            {
                if (parameters is null || !parameters.Value.TryGetProperty("path", out var pathParameters)) return requestPath;

                foreach (var param in pathParameters.EnumerateObject())
                {
                    requestPath = requestPath.Replace($"{{{param.Name}}}", param.Value.ToString());
                }

                return requestPath;
            }

            private static List<string>? ProcessQuery(JsonElement? parameters)
            {
                if (parameters is null || !parameters.Value.TryGetProperty("query", out var queryParameters)) return null;

                var queryParams = new List<string>();

                foreach (var param in queryParameters.EnumerateObject())
                {
                    queryParams.Add($"{param.Name}={Uri.EscapeDataString(param.Value.ToString())}");
                }

                return queryParams;
            }

            private static void ProcessHeader(HttpRequestMessage request, JsonElement? parameters)
            {
                if (parameters is null || !parameters.Value.TryGetProperty("header", out var headerParameters)) return;

                foreach (var header in headerParameters.EnumerateObject())
                {
                    request.Headers.Add(header.Name, header.Value.ToString());
                }
            }

            private static void ProcessJson(HttpRequestMessage request, JsonElement? parameters)
            {
                if (parameters is null || !parameters.Value.TryGetProperty("json", out var json)) return;

                request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            }

            private static void ProcessForm(HttpRequestMessage request, JsonElement? parameters)
            {
                if (parameters is null || !parameters.Value.TryGetProperty("form", out var form)) return;

                var kvp = form.EnumerateObject().Select(p => new KeyValuePair<string, string>(p.Name, p.Value.ToString()));
                request.Content = new FormUrlEncodedContent(kvp);
            }
        }
    }
}
