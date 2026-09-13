using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("sys_roles")]
    public class SysRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        [NotMapped]
        public string Code { get => RoleCode; set => RoleCode = value; }
        [NotMapped]
        public string Name { get => RoleName; set => RoleName = value; }
    }
}
