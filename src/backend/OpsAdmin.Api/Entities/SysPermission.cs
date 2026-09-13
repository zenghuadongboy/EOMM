using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("sys_permissions")]
    public class SysPermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PermissionCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Method { get; set; } = "GET";

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        [NotMapped]
        public string Code { get => PermissionCode; set => PermissionCode = value; }
        [NotMapped]
        public string Name { get => PermissionName; set => PermissionName = value; }
    }

    [Table("sys_role_permissions")]
    public class SysRolePermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RoleId { get; set; }

        public int PermissionId { get; set; }
    }
}
