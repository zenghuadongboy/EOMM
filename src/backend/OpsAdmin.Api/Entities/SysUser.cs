using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("sys_users")]
    public class SysUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string RealName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "Enable";

        public DateTime CreateTime { get; set; } = DateTime.Now;

        [NotMapped]
        public string Username { get => UserName; set => UserName = value; }

        public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
    }
}
