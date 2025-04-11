using System.Reflection;
using System.Runtime.Loader;

namespace MCPP.Net.Core
{
    /// <summary>
    /// 管控程序集的加载和卸载，避免因外部引用导致无法卸载程序集
    /// </summary>
    public interface IToolAssemblyLoader
    {
        /// <summary>
        /// 加载程序集
        /// </summary>
        /// <remarks>
        /// 返回值不要包含 <see cref="Assembly"/>, <see cref="Type"/>, <see cref="AssemblyLoadContext"/> 等反射相关类型以及动态程序集中的类型实例，否则在卸载程序集时，可能因外部依赖导致无法卸载程序集
        /// </remarks>
        ToolLoadedDetail Load(string assemblyPath);

        /// <summary>
        /// 卸载程序集
        /// </summary>
        void Unload(string assemblyName);
    }
}
