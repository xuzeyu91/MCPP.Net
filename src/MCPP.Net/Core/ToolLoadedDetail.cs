using MCPP.Net.Models;

namespace MCPP.Net.Core
{
    /// <summary>
    /// <see cref="IToolAssemblyLoader.Load(string)"/> 的返回值，包含了加载的具体信息
    /// </summary>
    public readonly record struct ToolLoadedDetail(List<string> RegisteredMethods, List<ImportedTool> ImportedTools);
}
