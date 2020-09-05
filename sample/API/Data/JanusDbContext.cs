using AnyService.EntityFramework.ValueGenerators;
using AnyService.Security;
using API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace API.Data
{
    public class JanusDbContext : DbContext
    {
        public JanusDbContext(DbContextOptions<JanusDbContext> options)
           : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var guidConverter = new ValueConverter<string, Guid>(
               v => new Guid(v),
               v => v.ToString(),
               new ConverterMappingHints(valueGeneratorFactory: (p, t) => new GuidStringGenerator()));

            var dateTimeNullConverter = new ValueConverter<string, DateTime?>(
                v => v.HasValue() ? (DateTime.Parse(v) as DateTime?) : null,
                v => v.HasValue ? v.Value.ToIso8601() : null);

            modelBuilder.Entity<EntityPermission>(b =>
            {
                b.Property(u => u.Id).ValueGeneratedOnAdd();
                b.Property(u => u.PermissionKeys)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            });

            modelBuilder.Entity<UserPermissions>(b =>
            {
                b.Property(u => u.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.Property(u => u.Id)
                    .ValueGeneratedOnAdd()
                    .HasConversion(guidConverter); ;
            });
        }
    }
}
