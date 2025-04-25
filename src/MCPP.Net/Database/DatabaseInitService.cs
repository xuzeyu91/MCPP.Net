namespace MCPP.Net.Database
{
    /// <summary>
    /// 数据库初始化服务
    /// </summary>
    public class DatabaseInitService(IServiceScopeFactory scopeFactory)
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public async Task Init()
        {
            using var scope  = scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<McppDbContext>();

            if (await dbContext.Database.EnsureCreatedAsync())
            {
                await InsertDefaultDataAsync(dbContext);
            }
        }

        private Task InsertDefaultDataAsync(McppDbContext dbContext)
        {
            // 可以在这里完成一些初始化数据插入

            return Task.CompletedTask;
        }
    }
}
