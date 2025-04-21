# MCPP.Net

MCPP.Net是一个基于.NET 8的Model Context Protocol (MCP)服务器实现，支持将Swagger API动态转换为MCP工具，使AI工具能够通过MCP协议调用这些API。

![图片介绍](https://github.com/user-attachments/assets/1950471f-fd54-4bfc-ad60-fa3f9ea334e6)

## 项目简介

MCPP.Net允许你将任意OpenAPI/Swagger定义的RESTful API服务动态转换为符合Model Context Protocol规范的工具，实现AI模型与外部服务的无缝集成。项目基于.NET 8构建，提供简单易用的API接口和Swagger UI。

## 主要功能

- 🔄 **动态Swagger导入**：通过URL或本地文件导入Swagger/OpenAPI定义
- 🛠️ **自动工具生成**：将API端点自动转换为MCP工具方法
- 🔌 **即插即用**：导入后立即可用，无需重启服务
- 📝 **Swagger UI支持**：内置Swagger UI，便于API测试和文档查看
- 📋 **工具管理**：支持查看和删除已导入的工具
- 🔄 **MCP协议实现**：完整支持Model Context Protocol

## 技术栈

- .NET 9
- ASP.NET Core Web API
- ModelContextProtocol 库
- Swashbuckle.AspNetCore (Swagger)
- Newtonsoft.Json

## 快速开始

### 系统要求

- .NET 9 SDK或更高版本
- Windows/Linux/macOS

### 安装指南

1. 克隆仓库
   ```bash
   git clone https://github.com/yourusername/MCPP.Net.git
   cd MCPP.Net
   ```

2. 构建项目
   ```bash
   dotnet build src/MCPP.Net
   ```

3. 运行项目
   ```bash
   dotnet run --project src/MCPP.Net
   ```

4. 访问Swagger UI
   ```
   https://localhost:7103/swagger/index.html
   ```

## 使用说明

### 导入Swagger API

1. 通过POST请求到 `/api/Import/Import` 导入Swagger:
   ```json
   {
     "swaggerUrl": "https://petstore.swagger.io/v2/swagger.json",
     "sourceBaseUrl": "https://petstore.swagger.io/v2",
     "nameSpace": "PetStore",
     "className": "PetStoreApi"
   }
   ```

2. 系统将自动：
   - 下载并解析Swagger定义
   - 生成适配MCP协议的工具类
   - 注册到MCP服务器
   
3. 导入成功后，返回导入结果：
   ```json
   {
     "success": true,
     "apiCount": 20,
     "toolClassName": "PetStoreApi",
     "importedApis": ["getPet", "updatePet", "..."]
   }
   ```

### 管理已导入的工具

- 获取已导入工具列表：GET `/api/Import/GetImportedTools`
- 删除已导入工具：DELETE `/api/Import/DeleteImportedTool?nameSpace=PetStore&className=PetStoreApi`

### 连接MCP客户端

MCP客户端可以通过SSE连接到 `/sse` 端点，然后通过POST请求到 `/message` 端点发送消息。

## 项目结构

- **Controllers/**：API控制器
  - `ImportController.cs`：处理Swagger导入相关API
- **Services/**：核心服务
  - `SwaggerImportService.cs`：Swagger导入和动态类型生成服务
  - `ImportedToolsService.cs`：管理已导入的工具
  - `McpServerMethodRegistry.cs`：MCP服务器方法注册
- **Models/**：数据模型
  - `SwaggerImportModels.cs`：导入相关模型定义
- **ImportedSwaggers/**：存储已导入的Swagger定义
- **ImportedTools/**：存储已编译的工具类

## 贡献指南

欢迎提交问题报告和合并请求。对于重大更改，请先开issue讨论您想要更改的内容。

## 许可证

[MIT](LICENSE)
