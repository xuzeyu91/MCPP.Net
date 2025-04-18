using System.Reflection;
using MCPP.Net.Common.Options;
using SqlSugar;

namespace MCPP.Net.Repositories.Base
{
    public class SqlSugarHelper()
    {

        /// <summary>
        /// DB链接
        /// </summary>
        public static SqlSugarScope SqlScope()
        {

            string DBType = ConnectionOptions.DbType;
            string ConnectionString = ConnectionOptions.ConnectionStrings;

            var config = new ConnectionConfig()
            {
                ConnectionString = ConnectionString,
                InitKeyType = InitKeyType.Attribute,//从特性读取主键和自增列信息
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    //注意:  这儿AOP设置不能少
                    EntityService = (c, p) =>
                    {
                        /***高版C#写法***/
                        //支持string?和string  
                        if (p.IsPrimarykey == false && new NullabilityInfoContext()
                         .Create(c).WriteState is NullabilityState.Nullable)
                        {
                            p.IsNullable = true;
                        }
                    }
                }
            };
            DbType dbType = (DbType)Enum.Parse(typeof(DbType), DBType);
            config.DbType = dbType;
            var scope = new SqlSugarScope(config, db =>
            {
                db.Aop.OnLogExecuting = (sql, pars) =>
                {

                    string log = sql + "\r\n" +
                                 SqlScope().Utilities
                                     .SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value));

                    Console.WriteLine(log);

                };

                //SQL执行完
                db.Aop.OnLogExecuted = (sql, pars) =>
                {
                    string time = "time:" + db.Ado.SqlExecutionTime.ToString();
                    Console.WriteLine(time);
                };

            });
            return scope;
        }
    }
}