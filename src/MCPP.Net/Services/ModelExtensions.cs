using MCPP.Net.Database.Entities;
using MCPP.Net.Models.Import;
using MCPP.Net.Models.Tool;

namespace MCPP.Net.Services
{
    internal static class ModelExtensions
    {
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
            return new McpTool
            {
                ImportId = request.ImportId,
                Name = request.Name!,
                HttpMethod = request.HttpMethod,
                RequestPath = request.RequestPath,
                Description = request.Description,
                InputSchema = request.InputSchema,
                Enabled = true
            };
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
