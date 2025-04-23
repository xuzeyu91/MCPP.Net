using MCPP.Net.Database.Entities;
using MCPP.Net.Models.Import;
using MCPP.Net.Models.Tool;
using System.Text;
using System.Text.RegularExpressions;

namespace MCPP.Net.Services
{
    internal static class ModelExtensions
    {
        internal static readonly string EmptyInputSchema;

        static ModelExtensions()
        {
            const string EMPTY_INPUT_SCHEMA = """
            {
                "type": "object",
                "properties": {
                    "parameters": {
                        "type": "object",
                        "properties": {
                        },
                        "required": []
                    }
                },
                "required": ["parameters"]
            }
            """;
            EmptyInputSchema = Regex.Replace(EMPTY_INPUT_SCHEMA, @"\s+", string.Empty);
        }

        public static Import ToImport(this CreateImportRequest request)
        {
            return new Import
            {
                Name = request.Name,
                ImportFrom = request.ImportFrom,
                SourceBaseUrl = request.SourceBaseUrl,
                Description = request.Description,
                Json = request.Json,
                Enabled = true
            };
        }

        public static QueryImportDto ToDto(this Import import)
        {
            return new()
            {
                Id = import.Id,
                Name = import.Name,
                ImportFrom = import.ImportFrom,
                SourceBaseUrl = import.SourceBaseUrl,
                Description = import.Description,
                Json = import.Json,
                Enabled = import.Enabled,
                CreatedAt = import.CreatedAt,
                UpdatedAt = import.UpdatedAt
            };
        }

        public static UpdateImportRequest ToUpdate(this CreateImportRequest request, long id)
        {
            return new()
            {
                Id = id,
                Name = request.Name,
                ImportFrom = request.ImportFrom,
                SourceBaseUrl = request.SourceBaseUrl,
                Description = request.Description,
                Json = request.Json,
                Enabled = true
            };
        }

        public static void Update(this Import import, UpdateImportRequest request)
        {
            import.Name = request.Name;
            import.ImportFrom = request.ImportFrom;
            import.SourceBaseUrl = request.SourceBaseUrl;
            import.Description = request.Description;
            import.Json = request.Json;
            import.Enabled = request.Enabled;
            import.UpdatedAt = DateTime.UtcNow;
        }

        public static McpTool ToTool(this CreateToolRequest request)
        {
            var name = request.Name;
            if (string.IsNullOrEmpty(name))
            {
                var formatedPath = SnakeCaseFormatPath(request.RequestPath);
                name = $"{request.HttpMethod}_{formatedPath}";
            }
            return new McpTool
            {
                ImportId = request.ImportId,
                Name = name,
                HttpMethod = request.HttpMethod,
                RequestPath = request.RequestPath,
                Description = request.Description,
                InputSchema = string.IsNullOrEmpty(request.InputSchema) ? EmptyInputSchema : request.InputSchema,
                Enabled = true
            };

            // /api/users/{userId} -> api_users__userId
            static string SnakeCaseFormatPath(string path)
            {
                if (string.IsNullOrEmpty(path)) throw new ArgumentException("无法根据空的 Request Path 生成名称");

                var span = path.AsSpan();

                var builder = new StringBuilder();
                var segments = span.Split('/');
                foreach (var range in segments)
                {
                    if (range.Start.Equals(range.End)) continue;

                    builder.Append('_');
                    var segment = span[range];
                    if (segment.StartsWith('{'))
                    {
                        segment = segment[1..^1];
                        builder.Append('_');
                    }
                    builder.Append(segment);
                }

                return builder.ToString(1, builder.Length - 1);
            }
        }

        public static QueryToolDto ToDto(this McpTool tool)
        {
            return new()
            {
                Id = tool.Id,
                ImportId = tool.ImportId,
                Name = tool.Name,
                HttpMethod = tool.HttpMethod,
                RequestPath = tool.RequestPath,
                Description = tool.Description,
                InputSchema = tool.InputSchema,
                Enabled = tool.Enabled,
                CreatedAt = tool.CreatedAt,
                UpdatedAt = tool.UpdatedAt
            };
        }

        public static void Update(this McpTool tool, CreateToolRequest request, bool enabled)
        {
            tool.Name = request.Name!;
            tool.HttpMethod = request.HttpMethod;
            tool.RequestPath = request.RequestPath;
            tool.Description = request.Description;
            tool.InputSchema = request.InputSchema;
            tool.Enabled = enabled;
            tool.ImportId = request.ImportId;
            tool.UpdatedAt = DateTime.UtcNow;
        }
    }
}
