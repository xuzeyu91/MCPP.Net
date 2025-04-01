using MCPP.Net;
using MCPP.Net.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.Server;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 注册Swagger服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AntSK.Api", Version = "v1" });
    //添加Api层注释（true表示显示控制器注释）
    var xmlFile = $"MCPP.Net.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, true);

    c.DocInclusionPredicate((docName, apiDes) =>
    {
        if (!apiDes.TryGetMethodInfo(out MethodInfo method))
            return false;
        var version = method.DeclaringType.GetCustomAttributes(true).OfType<ApiExplorerSettingsAttribute>().Select(m => m.GroupName);
        if (docName == "v1" && !version.Any())
            return true;
        var actionVersion = method.GetCustomAttributes(true).OfType<ApiExplorerSettingsAttribute>().Select(m => m.GroupName);
        if (actionVersion.Any())
            return actionVersion.Any(v => v == docName);
        return version.Any(v => v == docName);
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Description = "Directly enter bearer {token} in the box below (note that there is a space between bearer and token)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference()
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        }, Array.Empty<string>()
                    }
                });
});

// 注册MCP服务
builder.Services.AddSingleton<IMcpServerMethodRegistry, McpServerMethodRegistry>();
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