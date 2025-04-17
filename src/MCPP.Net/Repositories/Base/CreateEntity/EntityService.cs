using MCPP.Net.Common.DependencyInjection;
using SqlSugar;
using ServiceLifetime = MCPP.Net.Common.DependencyInjection.ServiceLifetime;

namespace MCPP.Net.Repositories.Base.CreateEntity
{
    [ServiceDescription(typeof(IEntityService), ServiceLifetime.Scoped)]
    public class EntityService : IEntityService
    {
        public SqlSugarScope db = SqlSugarHelper.SqlScope();
        /// <summary>
        /// 生成实体类
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool CreateEntity(string entityName, string filePath)
        {
            try
            {
                db.DbFirst.IsCreateAttribute().Where(entityName).SettingClassTemplate(old =>
                {
                    return old.Replace("{Namespace}", "MCPP.Net.Repositories");//修改Namespace命名空间
                }).CreateClassFile(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
