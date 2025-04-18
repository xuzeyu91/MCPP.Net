using Microsoft.Extensions.AI;
using System.Reflection;
using System.Text.Json;

namespace MCPP.Net.UnsafeImports
{
    /// <summary>
    /// https://github.com/dotnet/extensions/blob/f1f17e642a685df7e87b805be1efe4729ff725e4/src/Libraries/Microsoft.Extensions.AI.Abstractions/Utilities/AIJsonUtilities.Defaults.cs#L43-L73
    /// </summary>
    public static class UnsafeAIJsonUtilities
    {
        private static Func<JsonSerializerOptions> _createDefaultOptions;

        static UnsafeAIJsonUtilities()
        {
            var method = typeof(AIJsonUtilities).GetMethod("CreateDefaultOptions", BindingFlags.Static | BindingFlags.NonPublic);
            if (method is null) throw new InvalidOperationException("无法找到 AIJsonUtilities.CreateDefaultOptions 方法，当前版本的代码实现或许发生了改变");

            _createDefaultOptions = method.CreateDelegate<Func<JsonSerializerOptions>>();
        }

        /// <summary>
        /// 调用私有方法 Microsoft.Extensions.AI.AIJsonUtilities.CreateDefaultOptions
        /// </summary>
        /// <returns></returns>
        public static JsonSerializerOptions CreateDefaultOptions() => _createDefaultOptions();

        // 本来准备使用 UnsafeAccessorAttribute 实现的，但是目前 UnsafeAccessorAttribute 不支持静态类型，等支持后直接改用 UnsafeAccessorAttribute
        //[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Microsoft.Extensions.AI.AIJsonUtilities")]
        //public extern static JsonSerializerOptions CreateDefaultOptions(AIJsonUtilities target);
    }
}
