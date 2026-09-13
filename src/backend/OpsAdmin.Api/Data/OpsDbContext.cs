using Microsoft.EntityFrameworkCore;
using OpsAdmin.Api.Entities;

namespace OpsAdmin.Api.Data
{
    public class OpsDbContext : DbContext
    {
        public OpsDbContext(DbContextOptions<OpsDbContext> options) : base(options) { }

        public DbSet<SysUser> Users { get; set; }
        public DbSet<SysRole> Roles { get; set; }
        public DbSet<SysUserRole> UserRoles { get; set; }
        public DbSet<SysPermission> Permissions { get; set; }
        public DbSet<SysRolePermission> RolePermissions { get; set; }
        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AlarmRule> AlarmRules { get; set; }
        public DbSet<AlarmRecord> AlarmRecords { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WorkOrderLog> WorkOrderLogs { get; set; }
        public DbSet<OperationLog> OperationLogs { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SysUserRole 复合主键配置
            modelBuilder.Entity<SysUserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<SysUser>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<SysRole>()
                .HasIndex(r => r.RoleCode)
                .IsUnique();

            modelBuilder.Entity<AssetType>()
                .HasIndex(at => at.TypeCode)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.AssetNo)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.IpAddress);

            modelBuilder.Entity<AlarmRecord>()
                .HasIndex(ar => ar.AlarmNo)
                .IsUnique();

            modelBuilder.Entity<WorkOrder>()
                .HasIndex(w => w.WorkOrderNo)
                .IsUnique();
        }
    }
}
