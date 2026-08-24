using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("assets")]
    public class Asset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string AssetNo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Server, CloudHost, NetworkDevice, Storage, Database
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Server";

        [Required]
        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        public int Port { get; set; } = 80;

        [MaxLength(50)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Manager { get; set; } = string.Empty;

        // Online, Offline, Fault, Maintenance
        [MaxLength(20)]
        public string Status { get; set; } = "Online";

        [MaxLength(200)]
        public string HardwareSpec { get; set; } = string.Empty;

        public double CpuUsage { get; set; } = 0.0;

        public double MemoryUsage { get; set; } = 0.0;

        public double DiskUsage { get; set; } = 0.0;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public DateTime MaintenanceExpire { get; set; } = DateTime.Now.AddYears(3);

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
