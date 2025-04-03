using MCPP.Net.Models;
using ModelContextProtocol.Server;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private readonly IWebHostEnvironment _hostEnvironment;
        private static readonly Dictionary<string, ImportedTool> _importedTools = new Dictionary<string, ImportedTool>();
        private readonly string _storageDirectory;
        private readonly string _assemblyDirectory;

        public SwaggerImportService(
            ILogger<SwaggerImportService> logger,
            IMcpServerMethodRegistry methodRegistry,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _methodRegistry = methodRegistry;
            _httpClientFactory = httpClientFactory;
            _hostEnvironment = hostEnvironment;
            _storageDirectory = Path.Combine(_hostEnvironment.ContentRootPath, "ImportedSwaggers");
            _assemblyDirectory = _storageDirectory; // 使用相同目录存储程序集
            
            // 确保存储目录存在
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
            
            // 加载已保存的Swagger定义
            LoadSavedSwaggerDefinitions();
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
            // 优先使用用户提供的源服务器URL
            if (!string.IsNullOrEmpty(request.SourceBaseUrl))
            {
                baseUrl = request.SourceBaseUrl;
                _logger.LogInformation("使用用户提供的源服务器URL: {BaseUrl}", baseUrl);
            }
            else if (swaggerDoc["servers"] != null && swaggerDoc["servers"]!.Type == JTokenType.Array)
            {
                JArray servers = (JArray)swaggerDoc["servers"]!;
                if (servers.Count > 0 && servers[0]["url"] != null)
                {
                    baseUrl = servers[0]["url"]!.ToString();
                    _logger.LogInformation("从Swagger文档中获取服务器URL: {BaseUrl}", baseUrl);
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
                SwaggerSource = request.SwaggerUrl,
                SourceBaseUrl = request.SourceBaseUrl
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
            
            // 保存Swagger定义
            SaveSwaggerDefinition(request, swaggerJson);

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
                    string operationId = $"{request.NameSpace}_{request.ClassName}_{path.Replace("/", "_").Trim('_')}";
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
            
            // 加载程序集
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            Type dynamicType = assembly.GetType($"{request.NameSpace}.{request.ClassName}")!;
            
            _logger.LogInformation("已创建动态工具类型: {TypeName}", dynamicType.FullName);
            return dynamicType;
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
            
            // 获取ILProcessor
            ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();
            
            // 创建变量
            var stringBuilderVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(StringBuilder)));
            var httpClientVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(HttpClient)));
            var responseVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(HttpResponseMessage)));
            var contentVar = new VariableDefinition(moduleDefinition.ImportReference(typeof(string)));
            
            methodDefinition.Body.Variables.Add(stringBuilderVar);
            methodDefinition.Body.Variables.Add(httpClientVar);
            methodDefinition.Body.Variables.Add(responseVar);
            methodDefinition.Body.Variables.Add(contentVar);
            
            // 创建StringBuilder实例
            ilProcessor.Append(ilProcessor.Create(OpCodes.Newobj, moduleDefinition.ImportReference(typeof(StringBuilder).GetConstructor(Type.EmptyTypes))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, stringBuilderVar));
            
            // 添加API信息
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, $"API: {httpMethod} {path}\n"));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            // 添加API请求示例
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, $"\n示例请求:\n```\n{httpMethod} {fullUrl}\n"));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            // 添加请求头
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "Content-Type: application/json\n"));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            // 添加请求体示例
            if (hasRequestBody)
            {
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, $"\n{requestBodySchema}\n"));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            }
            
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "```\n\n参数值:\n"));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            // 添加参数信息
            for (int i = 0; i < parameterNames.Count; i++)
            {
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, $"- {parameterNames[i]}: "));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
                
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
                if (i < methodDefinition.Parameters.Count)
                {
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_S, methodDefinition.Parameters[i]));
                }
                else
                {
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, ""));
                }
                ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
                
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "\n"));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            }
            
            // 添加响应内容
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "\n响应内容:\n```\n"));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, contentVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, "\n```"));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) }))));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Pop));
            
            // 返回结果
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, stringBuilderVar));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Callvirt, moduleDefinition.ImportReference(typeof(StringBuilder).GetMethod("ToString", Type.EmptyTypes))));
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
        /// 保存Swagger定义
        /// </summary>
        private void SaveSwaggerDefinition(SwaggerImportRequest request, string swaggerJson)
        {
            try
            {
                string fileName = $"{request.NameSpace}.{request.ClassName}.json";
                string filePath = Path.Combine(_storageDirectory, fileName);
                
                // 创建包含swagger定义和请求信息的完整存储对象
                var storageObject = new SwaggerStorageItem
                {
                    Request = request,
                    SwaggerJson = swaggerJson,
                    ImportDate = DateTime.Now
                };
                
                string storageJson = JsonConvert.SerializeObject(storageObject, Formatting.Indented);
                File.WriteAllText(filePath, storageJson);
                
                _logger.LogInformation("已保存Swagger定义: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存Swagger定义失败: {Message}", ex.Message);
            }
        }
        
        /// <summary>
        /// 加载已保存的Swagger定义
        /// </summary>
        private void LoadSavedSwaggerDefinitions()
        {
            try
            {
                var swaggerFiles = Directory.GetFiles(_storageDirectory, "*.json");
                _logger.LogInformation("找到 {Count} 个已保存的Swagger定义", swaggerFiles.Length);
                
                foreach (var file in swaggerFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var storageItem = JsonConvert.DeserializeObject<SwaggerStorageItem>(json);
                        
                        if (storageItem != null)
                        {
                            _logger.LogInformation("加载Swagger定义: {NameSpace}.{ClassName}", 
                                storageItem.Request.NameSpace, storageItem.Request.ClassName);
                            
                            // 解析JSON并注册工具
                            JObject swaggerDoc = JObject.Parse(storageItem.SwaggerJson);
                            
                            // 获取服务器基础URL
                            string baseUrl = "";
                            // 优先使用用户提供的源服务器URL
                            if (!string.IsNullOrEmpty(storageItem.Request.SourceBaseUrl))
                            {
                                baseUrl = storageItem.Request.SourceBaseUrl;
                                _logger.LogInformation("使用用户提供的源服务器URL: {BaseUrl}", baseUrl);
                            }
                            else if (swaggerDoc["servers"] != null && swaggerDoc["servers"]!.Type == JTokenType.Array)
                            {
                                JArray servers = (JArray)swaggerDoc["servers"]!;
                                if (servers.Count > 0 && servers[0]["url"] != null)
                                {
                                    baseUrl = servers[0]["url"]!.ToString();
                                    _logger.LogInformation("从Swagger文档中获取服务器URL: {BaseUrl}", baseUrl);
                                }
                            }
                            
                            // 检查程序集是否已经存在
                            string assemblyFileName = $"{storageItem.Request.NameSpace}.{storageItem.Request.ClassName}.dll";
                            string assemblyPath = Path.Combine(_assemblyDirectory, assemblyFileName);
                            Type? toolType = null;
                            
                            if (File.Exists(assemblyPath))
                            {
                                try
                                {
                                    // 尝试加载已有的程序集
                                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                                    toolType = assembly.GetType($"{storageItem.Request.NameSpace}.{storageItem.Request.ClassName}");
                                    _logger.LogInformation("已加载现有工具程序集: {Path}", assemblyPath);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "加载已有程序集失败: {Path}, 将重新生成", assemblyPath);
                                }
                            }
                            
                            if (toolType == null)
                            {
                                // 动态生成API工具类
                                toolType = GenerateDynamicToolType(swaggerDoc, storageItem.Request, baseUrl);
                            }
                            
                            // 注册工具方法到MCP服务
                            List<string> registeredMethods = RegisterToolMethods(toolType);
                            
                            // 记录导入工具信息
                            var importedTool = new ImportedTool
                            {
                                NameSpace = storageItem.Request.NameSpace,
                                ClassName = storageItem.Request.ClassName,
                                ApiCount = registeredMethods.Count,
                                ImportDate = storageItem.ImportDate,
                                SwaggerSource = storageItem.Request.SwaggerUrl,
                                SourceBaseUrl = storageItem.Request.SourceBaseUrl
                            };
                            
                            string key = $"{storageItem.Request.NameSpace}.{storageItem.Request.ClassName}";
                            if (_importedTools.ContainsKey(key))
                            {
                                _importedTools[key] = importedTool;
                            }
                            else
                            {
                                _importedTools.Add(key, importedTool);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "加载Swagger定义失败: {File}, {Message}", file, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载Swagger定义失败: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// 获取所有已导入的工具
        /// </summary>
        /// <returns>已导入的工具列表</returns>
        public IReadOnlyList<ImportedTool> GetImportedTools()
        {
            return _importedTools.Values.ToList();
        }
        
        /// <summary>
        /// 获取所有已加载的动态工具类型
        /// </summary>
        /// <returns>已加载的工具类型列表</returns>
        public List<Type> GetDynamicToolTypes()
        {
            List<Type> toolTypes = new List<Type>();
            
            try
            {
                var swaggerFiles = Directory.GetFiles(_storageDirectory, "*.json");
                foreach (var file in swaggerFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var storageItem = JsonConvert.DeserializeObject<SwaggerStorageItem>(json);
                        
                        if (storageItem != null)
                        {
                            // 检查程序集是否已经存在
                            string assemblyFileName = $"{storageItem.Request.NameSpace}.{storageItem.Request.ClassName}.dll";
                            string assemblyPath = Path.Combine(_assemblyDirectory, assemblyFileName);
                            Type? toolType = null;
                            
                            if (File.Exists(assemblyPath))
                            {
                                try
                                {
                                    // 尝试加载已有的程序集
                                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                                    toolType = assembly.GetType($"{storageItem.Request.NameSpace}.{storageItem.Request.ClassName}");
                                    
                                    if (toolType != null)
                                    {
                                        toolTypes.Add(toolType);
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "加载已有程序集失败: {Path}", assemblyPath);
                                }
                            }
                            
                            // 如果程序集不存在或加载失败，重新生成
                            JObject swaggerDoc = JObject.Parse(storageItem.SwaggerJson);
                            
                            // 获取服务器基础URL
                            string baseUrl = "";
                            if (!string.IsNullOrEmpty(storageItem.Request.SourceBaseUrl))
                            {
                                baseUrl = storageItem.Request.SourceBaseUrl;
                            }
                            else if (swaggerDoc["servers"] != null && swaggerDoc["servers"]!.Type == JTokenType.Array)
                            {
                                JArray servers = (JArray)swaggerDoc["servers"]!;
                                if (servers.Count > 0 && servers[0]["url"] != null)
                                {
                                    baseUrl = servers[0]["url"]!.ToString();
                                }
                            }
                            
                            // 创建工具类型
                            toolType = GenerateDynamicToolType(swaggerDoc, storageItem.Request, baseUrl);
                            toolTypes.Add(toolType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "加载工具类型失败: {File}, {Message}", Path.GetFileName(file), ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取动态工具类型失败: {Message}", ex.Message);
            }
            
            return toolTypes;
        }
        
        /// <summary>
        /// 删除已导入的工具
        /// </summary>
        /// <param name="nameSpace">工具命名空间</param>
        /// <param name="className">类名</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteImportedTool(string nameSpace, string className)
        {
            string key = $"{nameSpace}.{className}";
            
            // 检查工具是否存在
            if (!_importedTools.ContainsKey(key))
            {
                _logger.LogWarning("尝试删除不存在的工具: {Key}", key);
                return false;
            }
            
            try
            {
                // 删除存储的JSON文件
                string jsonFilePath = Path.Combine(_storageDirectory, $"{key}.json");
                if (File.Exists(jsonFilePath))
                {
                    File.Delete(jsonFilePath);
                    _logger.LogInformation("已删除工具定义文件: {FilePath}", jsonFilePath);
                }
                
                // 删除程序集文件
                string assemblyFilePath = Path.Combine(_assemblyDirectory, $"{key}.dll");
                if (File.Exists(assemblyFilePath))
                {
                    File.Delete(assemblyFilePath);
                    _logger.LogInformation("已删除工具程序集文件: {FilePath}", assemblyFilePath);
                }
                
                // 从字典中移除记录
                _importedTools.Remove(key);
                
                _logger.LogInformation("已成功删除工具: {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除工具失败: {Key}, {Message}", key, ex.Message);
                throw;
            }
        }
    }
    
    /// <summary>
    /// Swagger存储项
    /// </summary>
    public class SwaggerStorageItem
    {
        /// <summary>
        /// 导入请求
        /// </summary>
        public SwaggerImportRequest Request { get; set; } = new SwaggerImportRequest();
        
        /// <summary>
        /// Swagger JSON内容
        /// </summary>
        public string SwaggerJson { get; set; } = string.Empty;
        
        /// <summary>
        /// 导入日期
        /// </summary>
        public DateTime ImportDate { get; set; }
    }
}