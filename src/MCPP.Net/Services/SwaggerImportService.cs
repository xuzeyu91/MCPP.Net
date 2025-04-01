using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using ModelContextProtocol.Server;
using System.Reflection;
using System.Reflection.Emit;

namespace MCPP.Net.Services
{
    /// <summary>
    /// 从Swagger定义导入API到MCP服务器的服务
    /// </summary>
    public class SwaggerImportService
    {
        private readonly ILogger<SwaggerImportService> _logger;
        private readonly IMcpServerMethodRegistry _methodRegistry;

        public SwaggerImportService(ILogger<SwaggerImportService> logger, IMcpServerMethodRegistry methodRegistry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _methodRegistry = methodRegistry ?? throw new ArgumentNullException(nameof(methodRegistry));
        }

        /// <summary>
        /// 从Swagger JSON或YAML文件导入API
        /// </summary>
        /// <param name="swaggerContent">Swagger文件内容</param>
        /// <returns>导入的API数量</returns>
        public async Task<int> ImportFromSwaggerAsync(string swaggerContent)
        {
            try
            {
                // 读取Swagger文档
                var openApiDocument = new OpenApiStringReader().Read(swaggerContent, out var diagnostic);
                
                if (diagnostic.Errors.Any())
                {
                    foreach (var error in diagnostic.Errors)
                    {
                        _logger.LogError("Swagger解析错误: {Error}", error.Message);
                    }
                    throw new InvalidOperationException("Swagger文档解析失败");
                }

                return await ImportFromOpenApiDocumentAsync(openApiDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入Swagger定义时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 从OpenApiDocument导入API
        /// </summary>
        /// <param name="document">OpenApi文档</param>
        /// <returns>导入的API数量</returns>
        public async Task<int> ImportFromOpenApiDocumentAsync(OpenApiDocument document)
        {
            int importedCount = 0;

            // 创建动态程序集
            var assemblyName = new AssemblyName($"DynamicSwaggerApi_{Guid.NewGuid()}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            // 为每个路径创建方法
            foreach (var path in document.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    var httpMethod = operation.Key.ToString();
                    var operationId = operation.Value.OperationId ?? $"{httpMethod}_{path.Key.Replace("/", "_")}";
                    
                    try
                    {
                        // 创建动态类型
                        var typeBuilder = moduleBuilder.DefineType(
                            $"SwaggerApi_{operationId}",
                            TypeAttributes.Public | TypeAttributes.Class);

                        // 定义方法
                        var methodBuilder = DefineApiMethod(typeBuilder, operation.Value, operationId, path.Key, httpMethod);

                        // 创建类型
                        var createdType = typeBuilder.CreateType();
                        
                        // 获取方法信息
                        var methodInfo = createdType.GetMethod(operationId);
                        if (methodInfo != null)
                        {
                            // 注册到MCP服务器
                            _methodRegistry.AddMethod(methodInfo);
                            importedCount++;
                            _logger.LogInformation("已导入API: {OperationId} ({HttpMethod} {Path})", 
                                operationId, httpMethod, path.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "导入操作时发生错误: {OperationId}", operationId);
                    }
                }
            }

            return importedCount;
        }

        /// <summary>
        /// 定义API方法
        /// </summary>
        private MethodBuilder DefineApiMethod(TypeBuilder typeBuilder, OpenApiOperation operation, 
            string operationId, string path, string httpMethod)
        {
            // 方法参数
            var parameters = new List<Type>();
            
            // 处理请求参数
            foreach (var parameter in operation.Parameters)
            {
                // 简化实现，所有参数都当作字符串处理
                parameters.Add(typeof(string));
            }

            // 创建方法
            var methodBuilder = typeBuilder.DefineMethod(
                operationId,
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(Task<object>),
                parameters.ToArray());

            // 生成方法体
            var il = methodBuilder.GetILGenerator();
            
            // 创建一个简单的方法实现，返回操作的说明
            // 在实际应用中，这里应该生成调用实际API的代码
            
            // 加载字符串常量
            il.Emit(OpCodes.Ldstr, $"API {operationId} ({httpMethod} {path}) - {operation.Description}");
            
            // 将字符串包装在Task.FromResult<object>中
            var taskFromResultMethod = typeof(Task).GetMethod("FromResult")
                .MakeGenericMethod(typeof(object));
            il.Emit(OpCodes.Call, taskFromResultMethod);
            
            // 返回
            il.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        /// <summary>
        /// 从URL导入Swagger定义
        /// </summary>
        /// <param name="url">Swagger定义URL</param>
        /// <returns>导入的API数量</returns>
        public async Task<int> ImportFromUrlAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var swaggerContent = await httpClient.GetStringAsync(url);
                return await ImportFromSwaggerAsync(swaggerContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从URL导入Swagger定义时发生错误: {Url}", url);
                throw;
            }
        }
    }
}