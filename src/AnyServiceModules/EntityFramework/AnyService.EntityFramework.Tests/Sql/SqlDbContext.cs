using AnyService.EntityFramework.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using static AnyService.EntityFramework.Tests.Sql.EfRepositorySqlServerTests;

namespace AnyService.EntityFramework.Tests
{
    public class SqlDbContext : DbContext
    {
        public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options)
        {
        }
        public DbSet<BulkInsertTestClass> BulkTestClasses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var converter = new ValueConverter<string, Guid>(
             v => new Guid(v),
             v => v.ToString(),
             new ConverterMappingHints(valueGeneratorFactory: (p, t) => new GuidStringGenerator()));

            modelBuilder.Entity<SqlBulkTestClass>(b =>
            {
                b.ToTable("SqlBulkTestClasses");
                b.Property(u => u.Id)
                   .ValueGeneratedOnAdd()
                   .HasConversion(converter);
            });
            modelBuilder.Entity<SqlBulkTestClass2>(b =>
            {
                b.ToTable("Table2");
                b.Property(u => u.Id)
                   .ValueGeneratedOnAdd()
                   .HasConversion(converter);
            });
        }
    }
}
