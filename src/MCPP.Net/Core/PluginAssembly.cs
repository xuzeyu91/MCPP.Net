using MCPP.Net.UnsafeImports;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace MCPP.Net.Core
{
    /// <summary>
    /// 程序集元数据信息，方便统一管理释放
    /// </summary>
    public class PluginAssembly(Assembly assembly, AssemblyLoadContext context) : IDisposable
    {
        /// <summary>
        /// 绑定到程序集的 <see cref="JsonSerializerOptions"/>
        /// </summary>
        /// <remarks>
        /// Microsoft.Extensions.Ai 内部通过 ConditionalWeakTable 缓存了 Tool 对应的 MethodInfo 信息，如果不释放将无法卸载程序集。
        /// ConditionalWeakTable 的 Key 为 <see cref="JsonSerializerOptions"/> 对象，所以可以将整个程序集的所有 Tools 对应到一个 <see cref="JsonSerializerOptions"/> 上，在卸载程序集前删除对象引用即可<br/>
        /// https://github.com/dotnet/extensions/blob/f1f17e642a685df7e87b805be1efe4729ff725e4/src/Libraries/Microsoft.Extensions.AI/Functions/AIFunctionFactory.cs#L219-L247
        /// </remarks>
        public JsonSerializerOptions? SerializerOptions { get; private set; } = UnsafeAIJsonUtilities.CreateDefaultOptions();

        /// <summary>
        /// 动态生成的程序集
        /// </summary>
        public Assembly Assembly => assembly;

        /// <inheritdoc/>
        public void Dispose()
        {
            SerializerOptions = null;
            context.Unload();
        }
    }
}
