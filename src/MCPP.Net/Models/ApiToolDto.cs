using MCPP.Net.Repositories;

namespace MCPP.Net.Models
{
    public class ApiToolDto
    {
        public string AppId { get; set; }
        /// <summary>
        /// 工具名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 工具描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// restful api 请求地址
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// 请求方法类型 1: GET 2: POST
        /// </summary>
        public int MethodType { get; set; }

        /// <summary>
        /// 请求参数,描述
        /// </summary>
        public string? InputSchema { get; set; }

        public tool ToTool()
        {
            return new tool()
            {
                appId = AppId,
                name = Name,
                description = Desc,
                endpoint = EndPoint,
                method_type = MethodType,
                input_schema = InputSchema,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };
        }
    }
}
