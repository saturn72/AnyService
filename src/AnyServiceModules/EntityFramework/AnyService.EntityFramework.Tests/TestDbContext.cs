using AnyService.EntityFramework.ValueGenerators;
using AnyService.Services.FileStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace AnyService.EntityFramework.Tests
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
            if (Database.IsSqlite())
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }
        public DbSet<TestClass> TestClasses { get; set; }
        public DbSet<FileModel> FileModels { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var converter = new ValueConverter<string, Guid>(
             v => new Guid(v),
             v => v.ToString(),
             new ConverterMappingHints(valueGeneratorFactory: (p, t) => new GuidStringGenerator()));

            modelBuilder.Entity<FileModel>(b => b.HasKey(u => u.Id));//.ValueGeneratedOnAdd());
            modelBuilder.Entity<TestClass>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<TestNestedClass>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());

            modelBuilder.Entity<BulkTestClass>(b =>
            {
                b.ToTable("BulkTestClasses");
                b.HasKey(u => u.Id);
                b.Property(u => u.Id)
                 //  .ValueGeneratedOnAdd()
                   .HasConversion(converter);
            });
        }
    }
}
