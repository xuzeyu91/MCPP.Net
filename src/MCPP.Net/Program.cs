using System.Configuration;
using System.Runtime.CompilerServices;
using MCPP.Net;
using MCPP.Net.Common.DependencyInjection;
using MCPP.Net.Common.Options;
using MCPP.Net.Core;
using MCPP.Net.Services;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// 获取 IConfiguration
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllers();

// 注册Swagger服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册MCP服务
builder.Services.AddSingleton<IMcpServerMethodRegistry, McpServerMethodRegistry>();

// 注册服务
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ImportedToolsService>();
builder.Services.AddSingleton<SwaggerImportService>();
builder.Services.AddSingleton<IToolAssemblyLoader, ToolAssemblyLoader>();
builder.Services.AddSingleton<IAssemblyBuilder, CecilAssemblyBuilder>();
builder.Services.AddScoped<IToolAppService, ToolAppService>();

//InitConfig(builder.Services);

//反射根据特性依赖注入
builder.Services.AddServicesFromAssemblies("MCPP.Net");

// 构建MCP服务
var mcpBuilder = builder.Services.AddMcpServer().WithHttpTransport();

// 注册程序集中的工具 - 必须在Build()之前完成
mcpBuilder.WithToolsFromAssembly();

mcpBuilder.UseToolsKeeper();

// mcpBuilder.WithDBTools(builder.Services.BuildServiceProvider());

// 构建应用
var app = builder.Build();

// 初始化ImportedToolsService，它会自动加载所有工具
var importedToolsService = app.Services.GetRequiredService<ImportedToolsService>();
Console.WriteLine($"已初始化ImportedToolsService，自动加载ImportedTools和ImportedSwaggers目录中的工具");



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

/// <summary>
/// 注入配置文件
/// </summary>
void InitConfig(IServiceCollection services)
{
    configuration.GetSection("ConnectionStrings").Get<ConnectionOptions>();
}