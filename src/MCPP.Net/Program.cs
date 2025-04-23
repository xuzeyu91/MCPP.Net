using MCPP.Net.Common.Middlewares;
using MCPP.Net.Core;
using MCPP.Net.Database;
using MCPP.Net.Services;
using MCPP.Net.Services.Impl;

var builder = WebApplication.CreateBuilder(args);
// 获取 IConfiguration
//var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllers();

// 注册Swagger服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddDbContext<McppDbContext>(ob => ob.Configure(builder.Configuration));

#region 之前通过程序集加载的相关代码（已注释）
// 注册MCP服务
//builder.Services.AddSingleton<IMcpServerMethodRegistry, McpServerMethodRegistry>();

// 注册服务
//builder.Services.AddSingleton<ImportedToolsService>();
//builder.Services.AddSingleton<SwaggerImportService>();
//builder.Services.AddSingleton<IToolAssemblyLoader, ToolAssemblyLoader>();
//builder.Services.AddSingleton<IAssemblyBuilder, CecilAssemblyBuilder>();
//builder.Services.AddScoped<IToolAppService, ToolAppService>();
//builder.Services.AddSingleton<DatabaseInitService>();

//InitConfig(builder.Services);

//反射根据特性依赖注入
//builder.Services.AddServicesFromAssemblies("MCPP.Net");
#endregion

builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IMcpToolService, McpToolService>();
builder.Services.AddSingleton<McpToolsKeeper>();
builder.Services.AddSingleton<DatabaseInitService>();

// 构建MCP服务
var mcpBuilder = builder.Services.AddMcpServer().WithHttpTransport();
mcpBuilder.WithToolsFromAssembly();
mcpBuilder.UseToolsKeeper();
// mcpBuilder.WithDBTools(builder.Services.BuildServiceProvider());

// 构建应用
var app = builder.Build();

// 初始化ImportedToolsService，它会自动加载所有工具
//var importedToolsService = app.Services.GetRequiredService<ImportedToolsService>();
//Console.WriteLine($"已初始化ImportedToolsService，自动加载ImportedTools和ImportedSwaggers目录中的工具");

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<ToolChangedHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCPP.Net API"); //注意中间段v1要和上面SwaggerDoc定义的名字保持一致
});

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = ["import.html"]
});

app.UseStaticFiles();

app.MapMcp("/mcp");

// 初始化（创建）数据库
await app.Services.GetRequiredService<DatabaseInitService>().Init();

await app.RunAsync();

/// <summary>
/// 注入配置文件
/// </summary>
//void InitConfig(IServiceCollection services)
//{
//    configuration.GetSection("ConnectionStrings").Get<ConnectionOptions>();
//}