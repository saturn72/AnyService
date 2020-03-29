using System;
using AnyService.Audity;
using AnyService.Core.Security;
using AnyService.EntityFramework;
using AnyService.SampleApp.Models;
using AnyService.Services.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace AnyService.SampleApp
{
    public sealed class SampleAppDbContext : DbContext, IAnyServiceDbContext
    {
        public SampleAppDbContext() : base()
        { }
        public SampleAppDbContext(DbContextOptions<SampleAppDbContext> options) : base(options)
        { }
        public DbSet<UserPermissions> UserPermissions { get; set; }
        public DbSet<DependentModel> DependentModel { get; set; }
        public DbSet<Dependent2> Dependent2s { get; set; }
        public DbSet<MultipartSampleModel> MultipartSampleModels { get; set; }
        public DbSet<FileModel> FileModels { get; set; }

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
            modelBuilder.Entity<UpdateRecord>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
        }
    }
}