using MCPP.Net.Database;
using MCPP.Net.Database.Entities;
using MCPP.Net.Models.Import;
using MCPP.Net.Models.Tool;
using Microsoft.EntityFrameworkCore;
using SqlSugar;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MCPP.Net.Services.Impl
{
    internal class ImportService(
        McppDbContext dbContext,
        IMcpToolService mcpToolService,
        IHttpClientFactory httpClientFactory,
        ILogger<ImportService> logger
        ) : IImportService
    {
        private static readonly JsonSerializerOptions _SerializerOptions = new() { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public async Task ImportAsync(CreateImportRequest request)
        {
            var sameNameImport = await dbContext.Imports.FirstOrDefaultAsync(x => x.Name == request.Name);
            if (sameNameImport != null)
            {
                if (sameNameImport.ImportFrom != request.ImportFrom) throw new InvalidOperationException($"已存在 Name 为 {request.Name} 的 import");

                await InternalUpdateAsync(sameNameImport, request.ToUpdate(sameNameImport.Id));
                return;
            }

            await DownloadSwaggerJsonAsync(request);
            var import = request.ToImport();
            dbContext.Imports.Add(import);

            await dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(import.Json))
            {
                var tools = SplitSwaggerToTools(import.Json, import.Id);

                await mcpToolService.ImportAsync(tools);
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<List<QueryImportDto>> ListAsync()
        {
            return await dbContext.Imports.Select(x => x.ToDto()).ToListAsync();
        }

        public async Task<QueryImportDto> GetAsync(long id)
        {
            var import = await dbContext.Imports.FindAsync(id) ?? throw new InvalidOperationException($"无法找到 ID 为 {id} 的 import");

            return import.ToDto();
        }

        public async Task UpdateAsync(UpdateImportRequest request)
        {
            var import = dbContext.Imports.Find(request.Id) ?? throw new InvalidOperationException($"无法找到 ID 为 {request.Id} 的 import");

            var sameNameImport = await dbContext.Imports.FirstOrDefaultAsync(x => x.Name == request.Name && x.Id != request.Id);
            if (sameNameImport != null) throw new InvalidOperationException($"已存在 Name 为 {request.Name} 的 import");

            await InternalUpdateAsync(import, request);
        }

        public async Task DeleteAsync(long id)
        {
            var import = await dbContext.Imports.FindAsync(id) ?? throw new InvalidOperationException($"无法找到 ID 为 {id} 的 import");
            dbContext.Imports.Remove(import);
            await dbContext.SaveChangesAsync();
        }

        private async Task InternalUpdateAsync(Import import, UpdateImportRequest request)
        {
            if (!string.IsNullOrEmpty(request.ImportFrom))
            {
                await DownloadSwaggerJsonAsync(request);
            }

            if (import.Json != request.Json)
            {
                if (string.IsNullOrEmpty(request.Json))
                {
                    await mcpToolService.DeleteByImportAsync(import.Id);
                }
                else
                {
                    var tools = SplitSwaggerToTools(request.Json, import.Id);
                    await mcpToolService.ImportAsync(tools);
                }
            }

            import.Update(request);
            await dbContext.SaveChangesAsync();
        }

        private static List<CreateToolRequest> SplitSwaggerToTools(string json, long importId)
        {
            var jsonNode = JsonNode.Parse(json);
            var paths = jsonNode?["paths"]?.AsObject();
            if (paths == null) return [];

            var tools = new List<CreateToolRequest>();

            foreach (var path in paths)
            {
                var pathUrl = path.Key;
                var pathOperations = path.Value?.AsObject();
                if (pathOperations == null) continue;

                foreach (var operation in pathOperations)
                {
                    var httpMethod = operation.Key.ToUpper();
                    var operationDetails = operation.Value;
                    if (operationDetails == null) continue;

                    var description = operationDetails["summary"]?.GetValue<string>() ?? operationDetails["description"]?.GetValue<string>() ?? string.Empty;

                    var tool = new CreateToolRequest
                    {
                        ImportId = importId,
                        HttpMethod = httpMethod,
                        RequestPath = pathUrl,
                        Description = description,
                        InputSchema = BuildInputSchema(operationDetails)
                    };

                    tools.Add(tool);
                }
            }

            return tools;
        }

        /// <summary>
        /// 根据 swagger json 中对参数的描述，构建 MCP Tool Input Schema
        /// </summary>
        /// <remarks>
        /// json schema 固定格式：
        /// <code>
        /// {
        ///     "type": "object",
        ///     "properties:": {
        ///         "parameters": {
        ///             "type": "object",
        ///             "properties": {
        ///                 "path": {
        ///                     "type": "object",
        ///                     "properties": { }
        ///                     "required": []
        ///                 },
        ///                 "query": { },
        ///                 "form": { },
        ///                 "body": { },
        ///                 "header": { }
        ///             },
        ///             "required": ["path", "query", "form", "body", "header"]
        ///         }
        ///     },
        ///     "required": ["parameters"]
        /// }
        /// </code>
        /// </remarks>
        private static string BuildInputSchema(JsonNode operation)
        {
            Dictionary<string, JsonObject>? mergedParameters = null;

            if (operation["parameters"] is JsonArray array)
            {
                mergedParameters = ExtractSchemaFromParameters(array);
            }

            if (operation["requestBody"] is { } requestBody)
            {
                mergedParameters ??= [];
                ExtractSchemaFromBody(requestBody, mergedParameters);
            }

            if (mergedParameters == null || mergedParameters.Count == 0) return string.Empty;

            var schema = MergeSchema(mergedParameters);

            return schema.ToJsonString(_SerializerOptions);

            static JsonObject MergeSchema(Dictionary<string, JsonObject> map)
            {
                var properties = new JsonObject();

                foreach (var item in map)
                {
                    properties[item.Key] = item.Value;
                }

                return new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = properties,
                            ["required"] = new JsonArray(map.Keys.Select(x => JsonValue.Create(x)).ToArray())
                        }
                    },
                    ["required"] = new JsonArray(["parameters"])
                };
            }
        }

        private static Dictionary<string, JsonObject> ExtractSchemaFromParameters(JsonArray array)
        {
            Dictionary<string, (JsonObject properties, List<string> required)> fromParameters = [];
            foreach (var item in array)
            {
                if (item == null) continue;

                var source = item["in"]?.GetValue<string>() ?? "query";
                if (!fromParameters.TryGetValue(source, out var parameters))
                {
                    parameters = ([], []);
                    fromParameters[source] = parameters;
                }

                var name = item["name"]!.GetValue<string>();
                var description = item["description"]?.GetValue<string>();
                var required = item["required"]?.GetValue<bool>() ?? false;
                var schema = item["schema"]!.DeepClone();
                schema["description"] = description;

                parameters.properties[name] = schema;
                if (required) parameters.required.Add(name);
            }

            return fromParameters.ToDictionary(x => x.Key, x => new JsonObject
            {
                ["type"] = "object",
                ["properties"] = x.Value.properties,
                ["required"] = new JsonArray(x.Value.required.Select(y => JsonValue.Create(y)).ToArray())
            });
        }

        private static void ExtractSchemaFromBody(JsonNode requestBody, Dictionary<string, JsonObject> mergedParameters)
        {
            var content = requestBody["content"]!;
            foreach (var item in content.AsObject())
            {
                var source = item.Key switch
                {
                    "application/json" => "json",
                    "application/x-www-form-urlencoded" => "form",
                    _ => null
                };
                if (source == null) continue;

                if (mergedParameters.ContainsKey(source)) throw new InvalidOperationException($"重复添加来自[{source}]的参数数据");

                mergedParameters[source] = SwaggerSchemaToStandard(item.Value!["schema"]!).AsObject();
                return;
            }

            var keys = content.AsObject().Select(pair => pair.Key);
            throw new NotSupportedException($"不支持的数据类型: {string.Join(", ", keys)}");
        }

        private static JsonNode SwaggerSchemaToStandard(JsonNode schemaNode)
        {
            var isArray = schemaNode["type"]?.GetValue<string>() == "array";
            var tempNode = schemaNode;
            if (isArray) schemaNode = schemaNode["items"]!;
            if (schemaNode["$ref"] is not { } @ref) return tempNode.DeepClone();

            var schemasNode = schemaNode.Root["components"]!["schemas"]!;

            var refValue = @ref.GetValue<string>();
            var index = refValue.LastIndexOf('/') + 1;
            var targetSchema = schemasNode[refValue[index..]]!;
            var standardSchema = targetSchema.DeepClone();
            var properties = standardSchema["properties"]!;
            foreach (var item in targetSchema["properties"]!.AsObject())
            {
                var schema = item.Value!;
                properties[item.Key] = SwaggerSchemaToStandard(schema);
                if (schema["description"] is { } description)
                {
                    properties[item.Key]!["description"] = description.DeepClone();
                }
            }

            return standardSchema;
        }

        private async Task DownloadSwaggerJsonAsync(CreateImportRequest request)
        {
            if (!string.IsNullOrEmpty(request.Json) || string.IsNullOrEmpty(request.ImportFrom)) return;

            using var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(request.ImportFrom);
            var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync();
            var document = await JsonDocument.ParseAsync(stream);

            using (var bufferStream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(bufferStream, new() { Indented = false }))
                {
                    document.WriteTo(writer);
                }

                // 使用紧凑格式，删除空格和换行，可以避免因格式问题让相同的 json 在判等时不相等
                request.Json = Encoding.UTF8.GetString(bufferStream.ToArray());
            }
        }
    }
}
