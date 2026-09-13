using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("sys_user_roles")]
    public class SysUserRole
    {
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public SysUser? User { get; set; }

        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public SysRole? Role { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
