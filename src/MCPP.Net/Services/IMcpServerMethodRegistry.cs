using System.Reflection;

namespace MCPP.Net.Services
{
    /// <summary>
    /// MCP服务器方法注册接口
    /// </summary>
    public interface IMcpServerMethodRegistry
    {
        /// <summary>
        /// 添加方法到MCP服务器
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        void AddMethod(MethodInfo methodInfo);

        void Clear();
    }
} 