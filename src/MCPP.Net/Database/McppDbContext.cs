using MCPP.Net.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace MCPP.Net.Database
{
    /// <summary>
    /// 应用数据库上下文
    /// </summary>
    public class McppDbContext : DbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        public McppDbContext(DbContextOptions<McppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 导入表
        /// </summary>
        public DbSet<Import> Imports { get; set; } = null!;

        /// <summary>
        /// MCP Tool信息表
        /// </summary>
        public DbSet<McpTool> McpTools { get; set; } = null!;

        /// <summary>
        /// 配置模型
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置McpTool实体与Import表的关系
            modelBuilder.Entity<McpTool>(entity =>
            {
                // 只配置实体关系，其他属性已通过数据注解配置
                entity.HasOne(e => e.Import)
                    .WithMany()
                    .HasForeignKey(e => e.ImportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
