using MCPP.Net.Models;
using ModelContextProtocol.Server;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace MCPP.Net.Core
{
    /// <summary>
    /// 使用 Mono.Cecil 动态生成程序集
    /// </summary>
    public class CecilAssemblyBuilder : IAssemblyBuilder
    {
        private readonly ILogger _logger;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly string _storageDirectory;
        private readonly string _assemblyDirectory;

        public CecilAssemblyBuilder(
            ILogger<CecilAssemblyBuilder> logger,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _storageDirectory = Path.Combine(_hostEnvironment.ContentRootPath, "ImportedSwaggers");
            _assemblyDirectory = _storageDirectory; // 使用相同目录存储程序集
        }

        public string Build(JObject swaggerDoc, SwaggerImportRequest request, string baseUrl)
        {
            string assemblyName = $"{request.NameSpace}.{request.ClassName}";
            string fileName = $"{assemblyName}.dll";
            string assemblyPath = Path.Combine(_assemblyDirectory, fileName);

            // 创建新的程序集定义
            var assemblyDefinition = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0)),
                assemblyName,
                ModuleKind.Dll);

            // 创建主模块
            ModuleDefinition moduleDefinition = assemblyDefinition.MainModule;

            // 添加必要的引用
            moduleDefinition.ImportReference(typeof(object));
            moduleDefinition.ImportReference(typeof(StringBuilder));
            moduleDefinition.ImportReference(typeof(McpServerToolTypeAttribute));
            moduleDefinition.ImportReference(typeof(McpServerToolAttribute));
            moduleDefinition.ImportReference(typeof(DescriptionAttribute));

            // 创建类型定义
            TypeDefinition typeDefinition = new TypeDefinition(
                request.NameSpace,
                request.ClassName,
                Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.Class,
                moduleDefinition.ImportReference(typeof(object)));

            // 添加默认构造函数
            var defaultCtor = new MethodDefinition(
                ".ctor",
                Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName,
                moduleDefinition.ImportReference(typeof(void)));

            var il = defaultCtor.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Call, moduleDefinition.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes))));
            il.Append(il.Create(OpCodes.Ret));

            typeDefinition.Methods.Add(defaultCtor);

            // 添加McpServerToolType特性
            var customAttribute = new CustomAttribute(
                moduleDefinition.ImportReference(typeof(McpServerToolTypeAttribute).GetConstructor(Type.EmptyTypes)));
            typeDefinition.CustomAttributes.Add(customAttribute);

            // 解析Swagger Paths并创建方法
            JObject paths = (JObject)swaggerDoc["paths"]!;

            foreach (var pathPair in paths)
            {
                string path = pathPair.Key;
                JObject operations = (JObject)pathPair.Value!;

                foreach (var operationPair in operations)
                {
                    string httpMethod = operationPair.Key.ToUpper();
                    JObject operation = (JObject)operationPair.Value!;

                    // 使用namespace + class + path生成operationId
                    string operationId = $"{request.NameSpace}_{request.ClassName}_{httpMethod}_{path.Replace("/", "_").Trim('_')}";
                    string summary = operation["summary"]?.ToString() ?? $"HTTP {httpMethod} {path}";
                    string description = operation["description"]?.ToString() ?? summary;

                    // 创建方法
                    CreateDynamicMethod(moduleDefinition, typeDefinition, operationId, path, httpMethod, summary, description, operation, baseUrl);
                }
            }

            // 将类型添加到模块
            moduleDefinition.Types.Add(typeDefinition);

            // 将程序集写入磁盘
            assemblyDefinition.Write(assemblyPath);

            return assemblyPath;
        }
        
        /// <summary>
         /// 创建动态方法
         /// </summary>
         /// <param name="moduleDefinition">模块定义</param>
         /// <param name="typeDefinition">类型定义</param>
         /// <param name="operationId">操作ID</param>
         /// <param name="path">API路径</param>
         /// <param name="httpMethod">HTTP方法</param>
         /// <param name="summary">摘要</param>
         /// <param name="description">描述</param>
         /// <param name="operation">操作定义</param>
         /// <param name="baseUrl">API基础URL</param>
        private void CreateDynamicMethod(
            ModuleDefinition moduleDefinition,
            TypeDefinition typeDefinition,
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

            List<string> parameterNames = new List<string>();
            List<string> parameterDescriptions = new List<string>();
            List<string> pathParams = new List<string>();
            List<string> queryParams = new List<string>();

            // 创建方法定义
            var methodDefinition = new MethodDefinition(
                methodName,
                Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static,
                moduleDefinition.ImportReference(typeof(string)));

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
                        var parameterDefinition = new ParameterDefinition(
                            normalizedName,
                            Mono.Cecil.ParameterAttributes.None,
                            moduleDefinition.ImportReference(typeof(string)));

                        methodDefinition.Parameters.Add(parameterDefinition);

                        // 为参数添加Description特性
                        string paramDescription = param["description"]?.ToString() ?? $"参数 {paramName}";
                        var descriptionAttr = new CustomAttribute(
                            moduleDefinition.ImportReference(
                                typeof(DescriptionAttribute).GetConstructor(new[] { typeof(string) })));
                        descriptionAttr.ConstructorArguments.Add(
                            new CustomAttributeArgument(moduleDefinition.ImportReference(typeof(string)), paramDescription));
                        parameterDefinition.CustomAttributes.Add(descriptionAttr);

                        parameterNames.Add(normalizedName);
                        parameterDescriptions.Add(paramDescription);

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
            string requestBodyDescription = "请求体 (JSON格式)";

            if (requestBody != null)
            {
                hasRequestBody = true;
                var parameterDefinition = new ParameterDefinition(
                    "requestBody",
                    Mono.Cecil.ParameterAttributes.None,
                    moduleDefinition.ImportReference(typeof(string)));

                methodDefinition.Parameters.Add(parameterDefinition);

                // 尝试提取请求体Schema和描述
                if (requestBody["content"] is JObject content &&
                    content["application/json"] is JObject jsonContent &&
                    jsonContent["schema"] is JObject schema)
                {
                    requestBodySchema = schema.ToString(Formatting.Indented);

                    // 生成详细的请求体描述
                    var schemaDescription = new StringBuilder();
                    schemaDescription.AppendLine("请求体结构:");
                    GenerateSchemaDescription(schema, schemaDescription, 1);
                    requestBodyDescription = schemaDescription.ToString();
                }

                // 为请求体参数添加Description特性
                var descriptionAttr = new CustomAttribute(
                    moduleDefinition.ImportReference(
                        typeof(DescriptionAttribute).GetConstructor(new[] { typeof(string) })));
                descriptionAttr.ConstructorArguments.Add(
                    new CustomAttributeArgument(moduleDefinition.ImportReference(typeof(string)), requestBodyDescription));
                parameterDefinition.CustomAttributes.Add(descriptionAttr);

                parameterNames.Add("requestBody");
                parameterDescriptions.Add(requestBodyDescription);
            }

            // 添加McpServerTool特性
            var mcpServerToolAttr = new CustomAttribute(
                moduleDefinition.ImportReference(
                    typeof(McpServerToolAttribute).GetConstructor(Type.EmptyTypes)));

            // 设置Name属性
            var nameProperty = typeof(McpServerToolAttribute).GetProperty("Name");
            if (nameProperty != null && nameProperty.GetSetMethod() != null)
            {
                mcpServerToolAttr.Properties.Add(
                    new Mono.Cecil.CustomAttributeNamedArgument(
                        "Name",
                        new CustomAttributeArgument(moduleDefinition.ImportReference(typeof(string)), methodName.ToLower())));
            }

            methodDefinition.CustomAttributes.Add(mcpServerToolAttr);

            // 添加Description特性
            var methodDescAttr = new CustomAttribute(
                moduleDefinition.ImportReference(
                    typeof(DescriptionAttribute).GetConstructor(new[] { typeof(string) })));
            methodDescAttr.ConstructorArguments.Add(
                new CustomAttributeArgument(moduleDefinition.ImportReference(typeof(string)), description));
            methodDefinition.CustomAttributes.Add(methodDescAttr);

            // 生成示例HTTP请求代码
            string fullPath = path;
            int placeholderIndex = 0;
            foreach (var pathParam in pathParams)
            {
                // 获取规范化的参数名
                string normalizedName = NormalizeParameterName(pathParam);
                // 使用简单的字符串替换，添加占位符
                fullPath = fullPath.Replace($"{{{pathParam}}}", $"{{{placeholderIndex++}}}");
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
                    string paramName = queryParams[i];
                    string normalizedName = NormalizeParameterName(paramName);
                    queryString += $"{paramName}={{{placeholderIndex++}}}";
                }
            }

            string fullUrl = baseUrl + fullPath + queryString;

            // 获取ILProcessor
            ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();

            // 创建变量
            var httpClientVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(HttpClient)));
            var responseVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(HttpResponseMessage)));
            var contentVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(string)));
            var formattedUrlVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(string)));

            methodDefinition.Body.Variables.Add(httpClientVar);
            methodDefinition.Body.Variables.Add(responseVar);
            methodDefinition.Body.Variables.Add(contentVar);
            methodDefinition.Body.Variables.Add(formattedUrlVar);

            // 格式化 URL，替换路径参数和查询参数
            if (pathParams.Count > 0 || queryParams.Count > 0)
            {
                // 使用 string.Format 方法格式化 URL
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, fullUrl));

                // 添加路径参数
                foreach (var pathParam in pathParams)
                {
                    string normalizedName = NormalizeParameterName(pathParam);
                    int paramIndex = FindParameterIndex(methodDefinition.Parameters, normalizedName);
                    if (paramIndex >= 0)
                    {
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg, paramIndex));
                    }
                    else
                    {
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, ""));
                    }
                }

                // 添加查询参数
                foreach (var queryParam in queryParams)
                {
                    string normalizedName = NormalizeParameterName(queryParam);
                    int paramIndex = FindParameterIndex(methodDefinition.Parameters, normalizedName);
                    if (paramIndex >= 0)
                    {
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg, paramIndex));
                    }
                    else
                    {
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, ""));
                    }
                }

                // 调用 string.Format 方法
                var stringFormatMethod = moduleDefinition.ImportReference(
                    typeof(string).GetMethod("Format", new Type[] {
                        typeof(string),
                        typeof(object),
                        typeof(object),
                        typeof(object)
                    }));

                if (pathParams.Count + queryParams.Count == 1)
                {
                    stringFormatMethod = moduleDefinition.ImportReference(
                        typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object) }));
                }
                else if (pathParams.Count + queryParams.Count == 2)
                {
                    stringFormatMethod = moduleDefinition.ImportReference(
                        typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object), typeof(object) }));
                }
                else if (pathParams.Count + queryParams.Count == 3)
                {
                    stringFormatMethod = moduleDefinition.ImportReference(
                        typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object), typeof(object), typeof(object) }));
                }
                else if (pathParams.Count + queryParams.Count > 3)
                {
                    // 对于更多参数，使用 params 版本的 Format 方法
                    stringFormatMethod = moduleDefinition.ImportReference(
                        typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object[]) }));

                    // 创建数组
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, pathParams.Count + queryParams.Count));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Newarr, moduleDefinition.ImportReference(typeof(object))));

                    // 填充数组
                    int index = 0;
                    foreach (var pathParam in pathParams)
                    {
                        string normalizedName = NormalizeParameterName(pathParam);
                        int paramIndex = FindParameterIndex(methodDefinition.Parameters, normalizedName);

                        ilProcessor.Append(ilProcessor.Create(OpCodes.Dup));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, index));

                        if (paramIndex >= 0)
                        {
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg, paramIndex));
                        }
                        else
                        {
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, ""));
                        }

                        ilProcessor.Append(ilProcessor.Create(OpCodes.Stelem_Ref));
                        index++;
                    }

                    foreach (var queryParam in queryParams)
                    {
                        string normalizedName = NormalizeParameterName(queryParam);
                        int paramIndex = FindParameterIndex(methodDefinition.Parameters, normalizedName);

                        ilProcessor.Append(ilProcessor.Create(OpCodes.Dup));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldc_I4, index));

                        if (paramIndex >= 0)
                        {
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg, paramIndex));
                        }
                        else
                        {
                            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, ""));
                        }

                        ilProcessor.Append(ilProcessor.Create(OpCodes.Stelem_Ref));
                        index++;
                    }
                }

                ilProcessor.Append(ilProcessor.Create(OpCodes.Call, stringFormatMethod));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, formattedUrlVar));
            }
            else
            {
                // 如果没有参数，直接使用 fullUrl
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, fullUrl));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, formattedUrlVar));
            }

            // ----------------- 新增发送 HTTP 请求的 IL代码 -----------------
            // 创建 HttpClient 实例
            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, moduleDefinition.ImportReference(typeof(HttpClient).GetConstructor(Type.EmptyTypes))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, httpClientVar));

            // 根据HTTP方法处理不同类型的请求
            switch (httpMethod.ToUpper())
            {
                case "GET":
                    // 发送 GET 请求
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, httpClientVar));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, formattedUrlVar));
                    var getAsyncMethod = moduleDefinition.ImportReference(typeof(HttpClient).GetMethod("GetAsync", new Type[] { typeof(string) }));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, getAsyncMethod));
                    break;

                case "DELETE":
                    // 发送 DELETE 请求
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, httpClientVar));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, formattedUrlVar));
                    var deleteAsyncMethod = moduleDefinition.ImportReference(typeof(HttpClient).GetMethod("DeleteAsync", new Type[] { typeof(string) }));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, deleteAsyncMethod));
                    break;

                case "POST":
                case "PUT":
                case "PATCH":
                    // 创建 HttpContent 用于请求内容
                    var requestContentVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(StringContent)));
                    methodDefinition.Body.Variables.Add(requestContentVar);

                    if (hasRequestBody)
                    {
                        // 创建 StringContent 实例
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg, methodDefinition.Parameters.Count - 1)); // 最后一个参数是requestBody
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Call, moduleDefinition.ImportReference(typeof(Encoding).GetProperty("UTF8").GetGetMethod())));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "application/json"));
                        var stringContentCtor = moduleDefinition.ImportReference(
                            typeof(StringContent).GetConstructor(new Type[] { typeof(string), typeof(Encoding), typeof(string) }));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, stringContentCtor));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, requestContentVar));
                    }
                    else
                    {
                        // 创建空的 StringContent
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "{}"));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Call, moduleDefinition.ImportReference(typeof(Encoding).GetProperty("UTF8").GetGetMethod())));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "application/json"));
                        var stringContentCtor = moduleDefinition.ImportReference(
                            typeof(StringContent).GetConstructor(new Type[] { typeof(string), typeof(Encoding), typeof(string) }));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, stringContentCtor));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, requestContentVar));
                    }

                    // 根据不同的HTTP方法选择相应的请求方式
                    if (httpMethod.ToUpper() == "POST")
                    {
                        // 发送 POST 请求
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, httpClientVar));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, formattedUrlVar));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, requestContentVar));
                        var postAsyncMethod = moduleDefinition.ImportReference(
                            typeof(HttpClient).GetMethod("PostAsync", new Type[] { typeof(string), typeof(HttpContent) }));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, postAsyncMethod));
                    }
                    else if (httpMethod.ToUpper() == "PUT")
                    {
                        // 发送 PUT 请求
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, httpClientVar));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, formattedUrlVar));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, requestContentVar));
                        var putAsyncMethod = moduleDefinition.ImportReference(
                            typeof(HttpClient).GetMethod("PutAsync", new Type[] { typeof(string), typeof(HttpContent) }));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, putAsyncMethod));
                    }
                    else // PATCH 和其他方法
                    {
                        // 对于 PATCH 方法，使用 SendAsync 方法
                        // 先创建一个临时变量存储HttpRequestMessage
                        var requestMessageVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(HttpRequestMessage)));
                        methodDefinition.Body.Variables.Add(requestMessageVar);

                        // 创建HttpMethod.Patch
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Call, moduleDefinition.ImportReference(
                            typeof(HttpMethod).GetProperty("Patch").GetGetMethod())));

                        // 加载URL
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, formattedUrlVar));

                        // 创建 HttpRequestMessage
                        var httpRequestMessageCtor = moduleDefinition.ImportReference(
                            typeof(HttpRequestMessage).GetConstructor(new Type[] { typeof(HttpMethod), typeof(string) }));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, httpRequestMessageCtor));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, requestMessageVar));

                        // 设置请求内容
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, requestMessageVar));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, requestContentVar));

                        var contentSetter = moduleDefinition.ImportReference(
                            typeof(HttpRequestMessage).GetProperty("Content").GetSetMethod());
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, contentSetter));

                        // 发送请求
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, httpClientVar));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, requestMessageVar));

                        var sendAsyncMethod = moduleDefinition.ImportReference(
                            typeof(HttpClient).GetMethod("SendAsync", new Type[] { typeof(HttpRequestMessage) }));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, sendAsyncMethod));
                    }
                    break;

                default:
                    // 默认使用GET请求
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, httpClientVar));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, formattedUrlVar));
                    var defaultGetAsyncMethod = moduleDefinition.ImportReference(typeof(HttpClient).GetMethod("GetAsync", new Type[] { typeof(string) }));
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, defaultGetAsyncMethod));
                    break;
            }

            // 调用 Task<HttpResponseMessage>.Result 获取响应结果
            var taskResponseResultGetter = moduleDefinition.ImportReference(
                typeof(System.Threading.Tasks.Task<HttpResponseMessage>).GetProperty("Result").GetGetMethod());
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, taskResponseResultGetter));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, responseVar));

            // 从响应中获取 HttpContent
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, responseVar));
            var getContentGetter = moduleDefinition.ImportReference(
                typeof(HttpResponseMessage).GetProperty("Content").GetGetMethod());
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, getContentGetter));

            // 调用 HttpContent.ReadAsStringAsync 并同步获取响应体字符串
            var readAsStringAsyncMethod = moduleDefinition.ImportReference(
                typeof(HttpContent).GetMethod("ReadAsStringAsync", Type.EmptyTypes));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, readAsStringAsyncMethod));
            var taskStringResultGetter = moduleDefinition.ImportReference(
                typeof(System.Threading.Tasks.Task<string>).GetProperty("Result").GetGetMethod());
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, taskStringResultGetter));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, contentVar));
            // ----------------- 结束发送 HTTP 请求的 IL代码 -----------------

            // 将响应体字符串加载到栈顶，并返回
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, contentVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

            // 将方法添加到类型
            typeDefinition.Methods.Add(methodDefinition);
        }

        /// <summary>
        /// 生成Schema的描述信息
        /// </summary>
        private void GenerateSchemaDescription(JObject schema, StringBuilder description, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);

            if (schema["type"]?.ToString() == "object")
            {
                if (schema["properties"] is JObject properties)
                {
                    foreach (var prop in properties)
                    {
                        string propName = prop.Key;
                        JObject propSchema = (JObject)prop.Value!;

                        // 添加属性名和类型
                        description.Append($"{indent}- {propName}: ");

                        if (propSchema["type"] != null)
                        {
                            description.Append(propSchema["type"]!.ToString());
                        }

                        // 添加描述
                        if (propSchema["description"] != null)
                        {
                            description.Append($" - {propSchema["description"]}");
                        }

                        description.AppendLine();

                        // 递归处理嵌套对象
                        if (propSchema["type"]?.ToString() == "object" && propSchema["properties"] != null)
                        {
                            GenerateSchemaDescription(propSchema, description, indentLevel + 1);
                        }
                        // 处理数组类型
                        else if (propSchema["type"]?.ToString() == "array" && propSchema["items"] != null)
                        {
                            description.Append($"{indent}  - 数组元素类型: ");
                            if (propSchema["items"]!["type"] != null)
                            {
                                description.AppendLine(propSchema["items"]!["type"]!.ToString());
                            }

                            if (propSchema["items"]!["type"]?.ToString() == "object" &&
                                propSchema["items"]!["properties"] != null)
                            {
                                GenerateSchemaDescription((JObject)propSchema["items"]!, description, indentLevel + 2);
                            }
                        }
                    }
                }
            }
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
        /// 查找参数在集合中的索引
        /// </summary>
        /// <param name="parameters">参数集合</param>
        /// <param name="name">参数名</param>
        /// <returns>参数索引，找不到返回-1</returns>
        private int FindParameterIndex(Collection<ParameterDefinition> parameters, string name)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

    }
}
