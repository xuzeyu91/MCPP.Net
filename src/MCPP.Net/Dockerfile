# 基于官方 .NET 9.0 SDK 镜像进行多阶段构建
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MCPP.Net.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# 数据库存储目录
VOLUME ["/app/Data"]
EXPOSE 7103
ENTRYPOINT ["dotnet", "MCPP.Net.dll"]
