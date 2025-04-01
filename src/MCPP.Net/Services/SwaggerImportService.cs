using MCPP.Net.Models;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace MCPP.Net.Services
{
    /// <summary>
    /// Swagger导入服务
    /// </summary>
    public class SwaggerImportService
    {
        private readonly ILogger<SwaggerImportService> _logger;
        private readonly IMcpServerMethodRegistry _methodRegistry;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Dictionary<string, ImportedTool> _importedTools = new Dictionary<string, ImportedTool>();

        public SwaggerImportService(
            ILogger<SwaggerImportService> logger,
            IMcpServerMethodRegistry methodRegistry,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _methodRegistry = methodRegistry;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 导入Swagger文档并生成MCP工具
        /// </summary>
        /// <param name="request">导入请求</param>
        /// <returns>导入结果</returns>
        public async Task<SwaggerImportResult> ImportSwaggerAsync(SwaggerImportRequest request)
        {
            _logger.LogInformation("开始处理Swagger导入: {Url}", request.SwaggerUrl);

            // 1. 获取Swagger JSON内容
            string swaggerJson = await GetSwaggerJsonAsync(request.SwaggerUrl);
            if (string.IsNullOrEmpty(swaggerJson))
            {
                throw new Exception($"无法获取Swagger文档: {request.SwaggerUrl}");
            }

            // 2. 解析Swagger JSON
            JObject swaggerDoc = JObject.Parse(swaggerJson);
            
            // 获取服务器基础URL
            string baseUrl = "";
            if (swaggerDoc["servers"] != null && swaggerDoc["servers"]!.Type == JTokenType.Array)
            {
                JArray servers = (JArray)swaggerDoc["servers"]!;
                if (servers.Count > 0 && servers[0]["url"] != null)
                {
                    baseUrl = servers[0]["url"]!.ToString();
                }
            }
            
            // 3. 动态生成API工具类
            Type toolType = GenerateDynamicToolType(swaggerDoc, request, baseUrl);
            
            // 4. 注册工具方法到MCP服务
            List<string> registeredMethods = RegisterToolMethods(toolType);

            // 5. 记录导入工具信息
            var importedTool = new ImportedTool
            {
                NameSpace = request.NameSpace,
                ClassName = request.ClassName,
                ApiCount = registeredMethods.Count,
                ImportDate = DateTime.Now,
                SwaggerSource = request.SwaggerUrl
            };
            
            string key = $"{request.NameSpace}.{request.ClassName}";
            if (_importedTools.ContainsKey(key))
            {
                _importedTools[key] = importedTool;
            }
            else
            {
                _importedTools.Add(key, importedTool);
            }

            // 6. 返回导入结果
            return new SwaggerImportResult
            {
                Success = true,
                ApiCount = registeredMethods.Count,
                ToolClassName = request.ClassName,
                ImportedApis = registeredMethods
            };
        }

        /// <summary>
        /// 获取Swagger JSON内容
        /// </summary>
        /// <param name="swaggerUrlOrPath">Swagger URL或本地文件路径</param>
        /// <returns>Swagger JSON内容</returns>
        private async Task<string> GetSwaggerJsonAsync(string swaggerUrlOrPath)
        {
            // 判断是否为URL
            if (Uri.TryCreate(swaggerUrlOrPath, UriKind.Absolute, out Uri? uri) && 
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // 通过HTTP请求获取Swagger文档
                var httpClient = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await httpClient.GetAsync(swaggerUrlOrPath);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                // 从本地文件读取Swagger文档
                if (!File.Exists(swaggerUrlOrPath))
                {
                    throw new FileNotFoundException($"找不到Swagger文件: {swaggerUrlOrPath}");
                }
                return await File.ReadAllTextAsync(swaggerUrlOrPath);
            }
        }

        /// <summary>
        /// 生成动态工具类型
        /// </summary>
        /// <param name="swaggerDoc">Swagger文档对象</param>
        /// <param name="request">导入请求</param>
        /// <param name="baseUrl">API基础URL</param>
        /// <returns>生成的动态类型</returns>
        private Type GenerateDynamicToolType(JObject swaggerDoc, SwaggerImportRequest request, string baseUrl)
        {
            // 创建AssemblyBuilder和ModuleBuilder
            AssemblyName assemblyName = new AssemblyName($"{request.NameSpace}.{request.ClassName}");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            // 创建TypeBuilder
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                $"{request.NameSpace}.{request.ClassName}", 
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

            // 添加McpServerToolType特性
            Type mcpServerToolTypeAttrType = typeof(McpServerToolTypeAttribute);
            ConstructorInfo mcpServerToolTypeCtorInfo = mcpServerToolTypeAttrType.GetConstructor(Type.EmptyTypes)!;
            CustomAttributeBuilder mcpServerToolTypeAttrBuilder = new CustomAttributeBuilder(mcpServerToolTypeCtorInfo, Array.Empty<object>());
            typeBuilder.SetCustomAttribute(mcpServerToolTypeAttrBuilder);

            // 解析Swagger Paths并创建方法
            JObject paths = (JObject)swaggerDoc["paths"]!;
            int methodCount = 0;

            foreach (var pathPair in paths)
            {
                string path = pathPair.Key;
                JObject operations = (JObject)pathPair.Value!;

                foreach (var operationPair in operations)
                {
                    string httpMethod = operationPair.Key.ToUpper();
                    JObject operation = (JObject)operationPair.Value!;

                    string operationId = operation["operationId"]?.ToString() ?? $"Operation{methodCount++}";
                    string summary = operation["summary"]?.ToString() ?? $"HTTP {httpMethod} {path}";
                    string description = operation["description"]?.ToString() ?? summary;

                    // 创建方法
                    CreateDynamicMethod(typeBuilder, operationId, path, httpMethod, summary, description, operation, baseUrl);
                }
            }

            // 生成类型
            Type dynamicType = typeBuilder.CreateType()!;
            _logger.LogInformation("已创建动态工具类型: {TypeName}", dynamicType.FullName);
            return dynamicType;
        }

        /// <summary>
        /// 创建动态方法
        /// </summary>
        /// <param name="typeBuilder">TypeBuilder</param>
        /// <param name="operationId">操作ID</param>
        /// <param name="path">API路径</param>
        /// <param name="httpMethod">HTTP方法</param>
        /// <param name="summary">摘要</param>
        /// <param name="description">描述</param>
        /// <param name="operation">操作定义</param>
        /// <param name="baseUrl">API基础URL</param>
        private void CreateDynamicMethod(
            TypeBuilder typeBuilder, 
            string operationId, 
            string path, 
            string httpMethod, 
            string summary, 
            string description, 
            JObject operation,
            string baseUrl)
        {
            // 规范化方法名
            string methodName = NormalizeMethodName(operationId);
            
            // 获取参数列表
            JArray? parameters = (JArray?)operation["parameters"];
            JObject? requestBody = (JObject?)operation["requestBody"];
            
            List<Type> parameterTypes = new List<Type>();
            List<string> parameterNames = new List<string>();
            List<string> parameterDescriptions = new List<string>();
            List<string> pathParams = new List<string>();
            List<string> queryParams = new List<string>();
            
            // 处理Path和Query参数
            if (parameters != null)
            {
                foreach (JObject param in parameters)
                {
                    string paramName = param["name"]?.ToString() ?? "";
                    string paramIn = param["in"]?.ToString() ?? "";
                    
                    if (paramIn == "path" || paramIn == "query")
                    {
                        string normalizedName = NormalizeParameterName(paramName);
                        parameterTypes.Add(typeof(string));
                        parameterNames.Add(normalizedName);
                        parameterDescriptions.Add(param["description"]?.ToString() ?? $"参数 {paramName}");
                        
                        if (paramIn == "path")
                        {
                            pathParams.Add(paramName);
                        }
                        else if (paramIn == "query")
                        {
                            queryParams.Add(paramName);
                        }
                    }
                }
            }
            
            // 处理请求体
            bool hasRequestBody = false;
            string requestBodySchema = "{}";
            
            if (requestBody != null)
            {
                hasRequestBody = true;
                parameterTypes.Add(typeof(string));
                parameterNames.Add("requestBody");
                parameterDescriptions.Add(requestBody["description"]?.ToString() ?? "请求体 (JSON格式)");
                
                // 尝试提取请求体Schema
                if (requestBody["content"] is JObject content && 
                    content["application/json"] is JObject jsonContent &&
                    jsonContent["schema"] is JObject schema)
                {
                    requestBodySchema = schema.ToString(Formatting.Indented);
                }
            }
            
            // 定义方法
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(string),
                parameterTypes.ToArray());
            
            // 设置参数名称
            for (int i = 0; i < parameterNames.Count; i++)
            {
                ParameterBuilder paramBuilder = methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameterNames[i]);
                
                // 为参数添加Description特性
                Type paramDescriptionAttrType = typeof(DescriptionAttribute);
                ConstructorInfo paramDescriptionCtorInfo = paramDescriptionAttrType.GetConstructor(new Type[] { typeof(string) })!;
                CustomAttributeBuilder paramDescriptionAttrBuilder = new CustomAttributeBuilder(
                    paramDescriptionCtorInfo, 
                    new object[] { parameterDescriptions[i] });
                paramBuilder.SetCustomAttribute(paramDescriptionAttrBuilder);
            }

            // 添加McpServerTool特性
            Type mcpServerToolAttrType = typeof(McpServerToolAttribute);
            ConstructorInfo mcpServerToolCtorInfo = mcpServerToolAttrType.GetConstructor(Type.EmptyTypes)!;
            
            CustomAttributeBuilder mcpServerToolAttrBuilder = new CustomAttributeBuilder(
                mcpServerToolCtorInfo, 
                Array.Empty<object>(),
                new PropertyInfo[] { mcpServerToolAttrType.GetProperty("Name")! },
                new object[] { methodName.ToLower() });
            methodBuilder.SetCustomAttribute(mcpServerToolAttrBuilder);

            // 添加Description特性
            Type descriptionAttrType = typeof(DescriptionAttribute);
            ConstructorInfo descriptionCtorInfo = descriptionAttrType.GetConstructor(new Type[] { typeof(string) })!;
            CustomAttributeBuilder descriptionAttrBuilder = new CustomAttributeBuilder(descriptionCtorInfo, new object[] { description });
            methodBuilder.SetCustomAttribute(descriptionAttrBuilder);

            // 生成示例HTTP请求代码
            string fullPath = path;
            foreach (var pathParam in pathParams)
            {
                fullPath = fullPath.Replace($"{{{pathParam}}}", $"\" + {NormalizeParameterName(pathParam)} + \"");
            }
            
            string queryString = "";
            if (queryParams.Count > 0)
            {
                queryString = "?";
                for (int i = 0; i < queryParams.Count; i++)
                {
                    if (i > 0)
                    {
                        queryString += "&";
                    }
                    string normalizedName = NormalizeParameterName(queryParams[i]);
                    queryString += $"{queryParams[i]}=\" + {normalizedName} + \"";
                }
            }
            
            string fullUrl = baseUrl + fullPath + queryString;
            
            // 生成方法实现
            ILGenerator il = methodBuilder.GetILGenerator();
            
            // 创建StringBuilder
            LocalBuilder stringBuilder = il.DeclareLocal(typeof(StringBuilder));
            il.Emit(OpCodes.Newobj, typeof(StringBuilder).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Stloc, stringBuilder);
            
            // 添加API信息
            il.Emit(OpCodes.Ldloc, stringBuilder);
            il.Emit(OpCodes.Ldstr, $"API: {httpMethod} {path}\n");
            il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
            il.Emit(OpCodes.Pop);
            
            // 添加API请求示例
            il.Emit(OpCodes.Ldloc, stringBuilder);
            il.Emit(OpCodes.Ldstr, $"\n示例请求:\n```\n{httpMethod} {fullUrl}\n");
            il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
            il.Emit(OpCodes.Pop);
            
            // 添加请求头
            il.Emit(OpCodes.Ldloc, stringBuilder);
            il.Emit(OpCodes.Ldstr, "Content-Type: application/json\n");
            il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
            il.Emit(OpCodes.Pop);
            
            // 添加请求体示例
            if (hasRequestBody)
            {
                il.Emit(OpCodes.Ldloc, stringBuilder);
                il.Emit(OpCodes.Ldstr, $"\n{requestBodySchema}\n");
                il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
                il.Emit(OpCodes.Pop);
            }
            
            il.Emit(OpCodes.Ldloc, stringBuilder);
            il.Emit(OpCodes.Ldstr, "```\n\n参数值:\n");
            il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
            il.Emit(OpCodes.Pop);
            
            // 添加参数信息
            for (int i = 0; i < parameterNames.Count; i++)
            {
                il.Emit(OpCodes.Ldloc, stringBuilder);
                il.Emit(OpCodes.Ldstr, $"- {parameterNames[i]}: ");
                il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
                il.Emit(OpCodes.Pop);
                
                il.Emit(OpCodes.Ldloc, stringBuilder);
                il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
                il.Emit(OpCodes.Pop);
                
                il.Emit(OpCodes.Ldloc, stringBuilder);
                il.Emit(OpCodes.Ldstr, "\n");
                il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!);
                il.Emit(OpCodes.Pop);
            }
            
            // 返回结果
            il.Emit(OpCodes.Ldloc, stringBuilder);
            il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("ToString", Type.EmptyTypes)!);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 规范化方法名
        /// </summary>
        /// <param name="operationId">操作ID</param>
        /// <returns>规范化的方法名</returns>
        private string NormalizeMethodName(string operationId)
        {
            // 移除非法字符，保留字母、数字和下划线
            string normalized = Regex.Replace(operationId, @"[^a-zA-Z0-9_]", "");
            
            // 确保以字母开头
            if (normalized.Length == 0 || !char.IsLetter(normalized[0]))
            {
                normalized = "Api" + normalized;
            }
            
            return normalized;
        }

        /// <summary>
        /// 规范化参数名
        /// </summary>
        /// <param name="paramName">参数名</param>
        /// <returns>规范化的参数名</returns>
        private string NormalizeParameterName(string paramName)
        {
            // 移除非法字符，保留字母、数字和下划线
            string normalized = Regex.Replace(paramName, @"[^a-zA-Z0-9_]", "");
            
            // 确保以字母开头
            if (normalized.Length == 0 || !char.IsLetter(normalized[0]))
            {
                normalized = "param" + normalized;
            }
            
            // 转换为小驼峰命名
            return char.ToLowerInvariant(normalized[0]) + normalized.Substring(1);
        }

        /// <summary>
        /// 注册工具方法到MCP服务
        /// </summary>
        /// <param name="toolType">工具类型</param>
        /// <returns>已注册的方法名列表</returns>
        private List<string> RegisterToolMethods(Type toolType)
        {
            List<string> registeredMethods = new List<string>();
            
            // 获取所有标记了[McpServerTool]特性的静态方法
            var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);
            
            foreach (var method in methods)
            {
                // 注册到MCP服务
                _methodRegistry.AddMethod(method);
                registeredMethods.Add(method.Name);
            }
            
            _logger.LogInformation("已注册 {Count} 个方法到MCP服务", registeredMethods.Count);
            return registeredMethods;
        }

        /// <summary>
        /// 获取所有已导入的工具
        /// </summary>
        /// <returns>已导入的工具列表</returns>
        public IReadOnlyList<ImportedTool> GetImportedTools()
        {
            return _importedTools.Values.ToList();
        }
    }
} 