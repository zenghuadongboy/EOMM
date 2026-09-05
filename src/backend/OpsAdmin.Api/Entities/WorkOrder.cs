using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("ops_work_orders")]
    public class WorkOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string WorkOrderNo { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public int AssetId { get; set; }

        [ForeignKey(nameof(AssetId))]
        public Asset? Asset { get; set; }

        public int? AlarmRecordId { get; set; }

        // Manual, AlarmTriggered, Inspection
        [MaxLength(30)]
        public string Source { get; set; } = "Manual";

        // Low, Medium, High, Critical
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        // Pending, Processing, Completed, Closed
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(50)]
        public string Creator { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Handler { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string ResolutionNotes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? FinishedAt { get; set; }
    }

    [Table("ops_work_order_logs")]
    public class WorkOrderLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int WorkOrderId { get; set; }

        [MaxLength(50)]
        public string OperatorName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PreviousStatus { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NewStatus { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ActionNote { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
