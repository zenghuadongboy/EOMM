using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("alarm_roles")]
    public class AlarmRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        public int AssetTypeId { get; set; }

        [ForeignKey(nameof(AssetTypeId))]
        public AssetType? AssetType { get; set; }

        [Required]
        [MaxLength(50)]
        public string Metric { get; set; } = "CpuUsage";

        [Required]
        [MaxLength(50)]
        public string Threshold { get; set; } = "80";

        // Info, Warning, Critical
        [MaxLength(20)]
        public string Severity { get; set; } = "Warning";

        // Enable, Disable
        [MaxLength(20)]
        public string Status { get; set; } = "Enable";

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        [NotMapped]
        public string Name { get => RuleName; set => RuleName = value; }
    }

    [Table("alarm_records")]
    public class AlarmRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string AlarmNo { get; set; } = string.Empty;

        public int AssetId { get; set; }

        [ForeignKey(nameof(AssetId))]
        public Asset? Asset { get; set; }

        public int RuleId { get; set; }

        [ForeignKey(nameof(RuleId))]
        public AlarmRule? Rule { get; set; }

        [MaxLength(200)]
        public string Content { get; set; } = string.Empty;

        // Info, Warning, Critical
        [MaxLength(20)]
        public string Severity { get; set; } = "Warning";

        // Pending, Processing, Resolved, Ignored
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime TriggeredAt { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string Handler { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ResolutionNotes { get; set; } = string.Empty;

        public DateTime? ResolvedAt { get; set; }
    }
}
