using ModelContextProtocol.Server;
using System.Runtime.CompilerServices;

namespace MCPP.Net.UnsafeImports
{
    internal class UnsafeMcpServerPrimitiveCollection<T> where T : IMcpServerPrimitive
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(RaiseChanged))]
        public extern static void RaiseChanged(McpServerPrimitiveCollection<T> target);
    }
}
