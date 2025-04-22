using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MCPP.Net.Core
{
    /// <summary>
    /// Tool 管理员，可以在运行时添加和删除 Tools
    /// </summary>
    public class McpToolsKeeper(ILogger<McpToolsKeeper> logger)
    {
        private const string DEFAULT_ID = "__default__";

        private ToolsCapability? _tools;
        private readonly ConcurrentDictionary<string, ImmutableDictionary<string, McpServerTool>> _map = [];

        /// <summary>
        /// 使用原始配置初始化<see cref="McpToolsKeeper"/>
        /// </summary>
        public void SetTools(ToolsCapability tools)
        {
            if (_tools is not null) throw new InvalidOperationException("Tools already been set.");
            
            _tools = tools;
            _map[DEFAULT_ID] = tools.ToolCollection!.ToImmutableDictionary(x => ((IMcpServerPrimitive)x).Name);
            
            logger.LogInformation($"McpToolsKeeper 已完成初始化，默认 Tools 共计 {tools.ToolCollection!.Count} 个");
        }

        internal void OnDbContextSavedChanges(object? sender, SavedChangesEventArgs e)
        {
            // todo: 需要通过某种机制通知客户端 tools 发生变化了
        }

        /// <summary>
        /// 添加一个新的 Tool 集合
        /// </summary>
        public void Add(string id, IEnumerable<McpServerTool> tools)
        {
            if (_map.ContainsKey(id))
            {
                Remove(id);
            }

            _map[id] = tools.ToImmutableDictionary(x => ((IMcpServerPrimitive)x).Name);

            logger.LogInformation($"新增 Tool 集合 [{id}]，共计包含 {tools.Count()} 个 Tools");

            // todo: 需要通过某种机制通知客户端 tools 发生变化了
            //if (_tools is not null)
            //{
            //    _tools.ListChanged = true;
            //}
        }

        /// <summary>
        /// 移除一个 Tool 集合
        /// </summary>
        public void Remove(string id)
        {
            if (_map.TryRemove(id, out _))
            {
                logger.LogInformation($"移出 Tool 集合 [{id}]");
            }
            else
            {
                logger.LogWarning($"Tool 集合 [{id}] 不存在，无法移除");
            }

            // todo: 需要通过某种机制通知客户端 tools 发生变化了
            //if (_tools is not null)
            //{
            //    _tools.ListChanged = true;
            //}
        }

        /// <summary>
        /// 获取所有 Tool 集合
        /// </summary>
        public ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams> context, CancellationToken token)
        {
            var result = new ListToolsResult();

            result.Tools.AddRange(_map.Values.SelectMany(x => x).Select(x => x.Value.ProtocolTool));

            return ValueTask.FromResult(result);
        }

        /// <summary>
        /// 调用指定的 Tool
        /// </summary>
        public ValueTask<CallToolResponse> CallToolHandler(RequestContext<CallToolRequestParams> context, CancellationToken token)
        {
            if (context.Params is not null)
            {
                foreach (var tools in _map.Values)
                {
                    if (tools.TryGetValue(context.Params.Name, out var tool)) return tool.InvokeAsync(context, token);
                }
            }
            throw new InvalidOperationException($"Unknown tool '{context.Params?.Name}'");
        }
    }
}
