using MCPP.Net.Models;
using Newtonsoft.Json.Linq;

namespace MCPP.Net.Core
{
    /// <summary>
    /// 程序集生成器标准接口
    /// </summary>
    public interface IAssemblyBuilder
    {
        /// <summary>
        /// 根据Swagger文档和请求信息动态生成程序集
        /// </summary>
        /// <returns>程序集文件路径</returns>
        string Build(JObject swaggerDoc, SwaggerImportRequest request, string baseUrl);
    }
}
