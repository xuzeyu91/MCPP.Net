using MCPP.Net.Core;
using MCPP.Net.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
        public McppDbContext(DbContextOptions<McppDbContext> options, McpToolsKeeper toolsKeeper) : base(options)
        {
            SavedChanges += toolsKeeper.OnDbContextSavedChanges;
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

            modelBuilder.Entity<McpTool>(entity =>
            {
                entity.HasOne(e => e.Import)
                    .WithMany(i => i.McpTools)
                    .HasForeignKey(e => e.ImportId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CreatedAt)
                    .ValueGeneratedOnAdd()
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<Import>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                    .ValueGeneratedOnAdd()
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
        }
    }
}
