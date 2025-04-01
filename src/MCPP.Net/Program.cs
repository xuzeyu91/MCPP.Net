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

builder.Services.AddMcpServer().WithToolsFromAssembly();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCPP.Net API"); //注意中间段v1要和上面SwaggerDoc定义的名字保持一致
});


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapMcp();
app.Run(); 