using System;
using System.Linq;
using System.Text;
using MCPP.Net.Models;
using SqlSugar;

namespace MCPP.Net.Repositories
{
    ///<summary>
    ///API定义表
    ///</summary>
    [SugarTable("tool")]
    public partial class tool
    {
        public tool()
        {


        }
        /// <summary>
        /// Desc:自增主键ID
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }

        /// <summary>
        /// Desc:应用id
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string appId { get; set; }

        /// <summary>
        /// Desc:tool名称
        /// Default:
        /// Nullable:False
        /// </summary>           
        public string name { get; set; }

        /// <summary>
        /// Desc:tool描述
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string description { get; set; }

        /// <summary>
        /// Desc:API的endpoint地址
        /// Default:
        /// Nullable:False
        /// </summary>           
        public string endpoint { get; set; }

        /// <summary>
        /// Desc:请求方式类型(如0:GET, 1:POST, 2:PUT, 3:DELETE等)
        /// Default:
        /// Nullable:False
        /// </summary>           
        public int method_type { get; set; }

        /// <summary>
        /// Desc:输入参数的JSON Schema定义
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string input_schema { get; set; }

        /// <summary>
        /// Desc:创建时间
        /// Default:CURRENT_TIMESTAMP
        /// Nullable:False
        /// </summary>           
        public DateTime created_at { get; set; }

        /// <summary>
        /// Desc:更新时间
        /// Default:CURRENT_TIMESTAMP
        /// Nullable:False
        /// </summary>           
        public DateTime updated_at { get; set; }

        public ApiToolDto ToApiToolDto()
        {
            //new tool()
            //{
            //    appId = input.AppId,
            //    name = input.Name,
            //    description = input.Desc,
            //    endpoint = input.EndPoint,
            //    method_type = input.MethodType,
            //    input_schema = input.InputSchema,
            //    created_at = DateTime.Now,
            //    updated_at = DateTime.Now
            //}

            return new ApiToolDto()
            {
                AppId = appId,
                Name = name,
                Desc = description,
                EndPoint = endpoint,
                MethodType = method_type,
                InputSchema = input_schema
            };
        }

    }
}
