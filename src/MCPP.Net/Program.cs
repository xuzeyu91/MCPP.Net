using MCPP.Net;
using MCPP.Net.Services;
using ModelContextProtocol.Server;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 注册Swagger服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册MCP服务
builder.Services.AddSingleton<IMcpServerMethodRegistry, McpServerMethodRegistry>();

// 注册Swagger导入服务
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SwaggerImportService>();
builder.Services.AddSingleton<ImportedToolsService>();

// 获取服务提供程序
using var serviceProvider = builder.Services.BuildServiceProvider();

// 先注册MCP服务
var mcpBuilder = builder.Services.AddMcpServer();

// 提前初始化ImportedToolsService，加载已编译的工具
var importedToolsService = serviceProvider.GetRequiredService<ImportedToolsService>();
Console.WriteLine($"已初始化ImportedToolsService，自动加载已编译的工具");

// 提前初始化SwaggerImportService，让它生成动态类型
var swaggerImportService = serviceProvider.GetRequiredService<SwaggerImportService>();
Console.WriteLine($"已初始化SwaggerImportService");

// 获取所有Swagger动态生成的工具类型
var dynamicToolTypes = swaggerImportService.GetDynamicToolTypes();
Console.WriteLine($"找到 {dynamicToolTypes.Count} 个Swagger动态工具类型");

// 添加动态生成的Swagger工具类型
mcpBuilder.WithSwaggerTools(dynamicToolTypes);
Console.WriteLine($"已注册Swagger动态工具类型");

// 最后注册程序集中的工具
mcpBuilder.WithToolsFromAssembly();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCPP.Net API"); //注意中间段v1要和上面SwaggerDoc定义的名字保持一致
});


//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapMcp();
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "import.html" }
});

app.UseStaticFiles();
app.Run(); 