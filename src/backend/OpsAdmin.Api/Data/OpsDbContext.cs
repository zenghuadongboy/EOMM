using Microsoft.EntityFrameworkCore;
using OpsAdmin.Api.Entities;

namespace OpsAdmin.Api.Data
{
    public class OpsDbContext : DbContext
    {
        public OpsDbContext(DbContextOptions<OpsDbContext> options) : base(options) { }

        public DbSet<SysUser> Users { get; set; }
        public DbSet<SysRole> Roles { get; set; }
        public DbSet<SysPermission> Permissions { get; set; }
        public DbSet<SysRolePermission> RolePermissions { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AlarmRule> AlarmRules { get; set; }
        public DbSet<AlarmRecord> AlarmRecords { get; set; }
        public DbSet<OperationLog> OperationLogs { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SysUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.AssetNo)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.IpAddress);

            modelBuilder.Entity<AlarmRecord>()
                .HasIndex(ar => ar.AlarmNo)
                .IsUnique();
        }
    }
}
