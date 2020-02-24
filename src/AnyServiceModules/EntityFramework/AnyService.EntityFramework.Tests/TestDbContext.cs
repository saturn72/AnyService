using AnyService.Services.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace AnyService.EntityFramework.Tests
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        { }
        public DbSet<TestClass> TestClasses { get; set; }
        public DbSet<FileModel> FileModels { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<TestClass>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<TestNestedClass>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
        }
    }
}
