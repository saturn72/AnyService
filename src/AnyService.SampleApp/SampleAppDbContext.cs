using System;
using AnyService.Audity;
using AnyService.Security;
using AnyService.SampleApp.Models;
using AnyService.Services.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace AnyService.SampleApp
{
    public sealed class SampleAppDbContext : DbContext
    {
        public SampleAppDbContext() : base()
        { }
        public SampleAppDbContext(DbContextOptions<SampleAppDbContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPermissions>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<EntityPermission>(b =>
            {
                b.Property(u => u.Id).ValueGeneratedOnAdd();
                b.Property(u => u.PermissionKeys)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            });
            modelBuilder.Entity<DependentModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<Dependent2>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<MultipartSampleModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<FileModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<CustomModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<AuditRecord>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<Stock>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
        }
    }
}