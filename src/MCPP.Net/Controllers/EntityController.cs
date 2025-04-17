#if DEBUG
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using MCPP.Net.Repositories.Base.CreateEntity;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace ISS.IPSA.AIMaaS.Controllers
{
    /// <summary>
    /// 生成实体
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EntityController(
        IEntityService _entity,
        IHostingEnvironment _hostingEnvironment
        ) : Controller
    {
        /// <summary>
        /// 生成实体类
        /// </summary>
        /// <param name="entityName">表名,生成目录默认在根目录</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CreateEntity(string entityName = null)
        {
            // 获取当前桌面用户名
            string userName = Environment.UserName;
            bool flag = false;
            if (!_hostingEnvironment.IsProduction())
            {
                if (entityName == null)
                    return Json("参数为空");
                string path = @$"C:\Users\{userName}\Desktop\entity\{entityName}";

                DirectoryInfo dir = new DirectoryInfo(path);
                if (dir.Exists)
                {
                    dir.Delete();
                }
                dir.Create();
                //创建实体
                flag = _entity.CreateEntity(entityName, path); //_entity.CreateEntity(entityName, _hostingEnvironment.ContentRootPath);

                //创建服务
                CreateRepositories(entityName, path);

                //创建接口
                CreateIRepositories(entityName, path);
            }
            return Ok(new
            {
                isSuccess = flag,
                filePath = @$"C:\Users\{userName}\Desktop\entity\"
            });
        }

        private static void CreateIRepositories(string entityName, string path)
        {
            using (var fileStream = new FileStream($"{path}\\I{entityName}_Repositories.cs", FileMode.CreateNew))
            {
                string repositories = $@"using MCPP.Net.Repositories;
namespace MCPP.Net.Repositories
{{
    public interface I{entityName}_Repositories :IRepository<{entityName}>
    {{
    }}
}}";
                byte[] data = Encoding.UTF8.GetBytes(repositories);//使用ASCII码将字符串转换为字节数据，所以一个字符占用一个字节
                fileStream.Write(data, 0, data.Length);
            }
        }

        private static void CreateRepositories(string entityName, string path)
        {
            using (var fileStream = new FileStream($"{path}\\{entityName}_Repositories.cs", FileMode.CreateNew))
            {
                string repositories = @$"using MCPP.Net.Repositories;
using SqlSugar;
namespace MCPP.Net.Repositories
{{
    [ServiceDescription(typeof(I{entityName}_Repositories), ServiceLifetime.Scoped)]
    public class {entityName}_Repositories : Repository<{entityName}>, I{entityName}_Repositories
    {{
    }}
}}";
                byte[] data = Encoding.UTF8.GetBytes(repositories);//使用ASCII码将字符串转换为字节数据，所以一个字符占用一个字节
                fileStream.Write(data, 0, data.Length);
            }
        }
    }
} 
#endif