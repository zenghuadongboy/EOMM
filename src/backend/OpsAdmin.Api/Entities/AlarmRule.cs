using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("alarm_rules")]
    public class AlarmRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Offline, CpuUsage, MemoryUsage, DiskUsage
        [Required]
        [MaxLength(50)]
        public string MetricType { get; set; } = "CpuUsage";

        // >, >=, ==, <, <=
        [MaxLength(10)]
        public string Operator { get; set; } = ">";

        public double Threshold { get; set; } = 80.0;

        // Info, Warning, Critical
        [MaxLength(20)]
        public string Severity { get; set; } = "Warning";

        public int IntervalSeconds { get; set; } = 60;

        public bool IsEnabled { get; set; } = true;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
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
