using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.Reflection;
using System.Text.Json;

namespace MCPP.Net.UnsafeImports
{
    /// <summary>
    /// https://github.com/modelcontextprotocol/csharp-sdk/blob/4c537ef86bd8bb10980962a8f0bac001453c5cd9/src/ModelContextProtocol/Server/AIFunctionMcpServerTool.cs
    /// </summary>
    public static class UnsafeAIFunctionMcpServerTool
    {
        private static Func<AIFunction, McpServerToolCreateOptions?, McpServerTool> _Create;
        private static Func<MethodInfo, McpServerToolCreateOptions?, AIFunctionFactoryOptions> _CreateAIFunctionFactoryOptions;
        private static Func<MethodInfo, McpServerToolCreateOptions?, McpServerToolCreateOptions> _DeriveOptions;

        static UnsafeAIFunctionMcpServerTool()
        {
            var typeAIFunctionMcpServerTool = typeof(McpServerTool).Assembly.GetType("ModelContextProtocol.Server.AIFunctionMcpServerTool");
            if (typeAIFunctionMcpServerTool is null) throw new InvalidOperationException("无法找到私有类型 ModelContextProtocol.Server.AIFunctionMcpServerTool，可能在当前版本已被移除");

            var methodCreate = typeAIFunctionMcpServerTool.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, [typeof(AIFunction), typeof(McpServerToolCreateOptions)]);
            if (methodCreate is null) throw new InvalidOperationException("无法找到 AIFunctionMcpServerTool.Create 方法，当前版本的代码实现或许发生了改变");
            _Create = methodCreate.CreateDelegate<Func<AIFunction, McpServerToolCreateOptions?, McpServerTool>>();

            var methodCreateAIFunctionFactoryOptions = typeAIFunctionMcpServerTool.GetMethod("CreateAIFunctionFactoryOptions", BindingFlags.Static | BindingFlags.NonPublic);
            if (methodCreateAIFunctionFactoryOptions is null) throw new InvalidOperationException("无法找到 AIFunctionMcpServerTool.CreateAIFunctionFactoryOptions 方法，当前版本的代码实现或许发生了改变");
            _CreateAIFunctionFactoryOptions = methodCreateAIFunctionFactoryOptions.CreateDelegate<Func<MethodInfo, McpServerToolCreateOptions?, AIFunctionFactoryOptions>>();

            var methodDeriveOptions = typeAIFunctionMcpServerTool.GetMethod("DeriveOptions", BindingFlags.Static | BindingFlags.NonPublic);
            if (methodDeriveOptions is null) throw new InvalidOperationException("无法找到 AIFunctionMcpServerTool.DeriveOptions 方法，当前版本的代码实现或许发生了改变");
            _DeriveOptions = methodDeriveOptions.CreateDelegate<Func<MethodInfo, McpServerToolCreateOptions?, McpServerToolCreateOptions>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Microsoft.Extensions.AI 内部会使用 JsonSerializerOptions，
        /// </remarks>
        public static McpServerTool Create(MethodInfo method, JsonSerializerOptions serializerOptions)
        {
            return Create(method, null, null, serializerOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="target"></param>
        /// <param name="options"></param>
        /// <param name="serializerOptions"></param>
        /// <returns></returns>
        public static McpServerTool Create(MethodInfo method, object? target, McpServerToolCreateOptions? options, JsonSerializerOptions? serializerOptions)
        {
            options = _DeriveOptions(method, options);

            var factoryOptions = _CreateAIFunctionFactoryOptions(method, options);
            factoryOptions.SerializerOptions = serializerOptions;

            var function = AIFunctionFactory.Create(method, target, factoryOptions);

            return _Create(function, options);
        }

        // 本来准备使用 UnsafeAccessorAttribute 实现的，但是目前 UnsafeAccessorAttribute 不支持私有类型，等支持后直接改用 UnsafeAccessorAttribute

        //[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ModelContextProtocol.Server.AIFunctionMcpServerTool.Create")]
        //private extern static McpServerTool Create(AIFunction function, McpServerToolCreateOptions? options);

        //[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ModelContextProtocol.Server.AIFunctionMcpServerTool.CreateAIFunctionFactoryOptions")]
        //private extern static AIFunctionFactoryOptions CreateAIFunctionFactoryOptions(MethodInfo method, McpServerToolCreateOptions? options);

        //[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ModelContextProtocol.Server.AIFunctionMcpServerTool.DeriveOptions")]
        //private extern static McpServerToolCreateOptions? DeriveOptions(MethodInfo method, McpServerToolCreateOptions? options);
    }
}
