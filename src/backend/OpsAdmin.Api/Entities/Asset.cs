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
        public string AssetName { get; set; } = string.Empty;

        public int AssetTypeId { get; set; }

        [ForeignKey(nameof(AssetTypeId))]
        public AssetType? AssetType { get; set; }

        [Required]
        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        // Online, Offline, Fault, Maintenance
        [MaxLength(20)]
        public string Status { get; set; } = "Online";

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        [NotMapped]
        public string Name { get => AssetName; set => AssetName = value; }

        [NotMapped]
        public string Type => AssetType?.TypeName ?? "Server";
    }
}
