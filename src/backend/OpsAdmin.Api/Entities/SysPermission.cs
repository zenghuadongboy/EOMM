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
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Menu, Button, Api
        [MaxLength(20)]
        public string Type { get; set; } = "Menu";

        [MaxLength(200)]
        public string Path { get; set; } = string.Empty;

        public int ParentId { get; set; } = 0;

        public int SortOrder { get; set; } = 0;
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
