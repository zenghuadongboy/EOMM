using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpsAdmin.Api.Entities
{
    [Table("work_orders")]
    public class WorkOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string WorkOrderNo { get; set; } = string.Empty;

        // Manual, AlarmTriggered, Inspection
        [MaxLength(20)]
        public string Source { get; set; } = "Manual";

        // Low, Medium, High, Critical
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        public int AssetId { get; set; }

        [ForeignKey(nameof(AssetId))]
        public Asset? Asset { get; set; }

        public int AlarmId { get; set; }

        [ForeignKey(nameof(AlarmId))]
        public AlarmRecord? AlarmRecord { get; set; }

        [MaxLength(50)]
        public string Handler { get; set; } = string.Empty;

        // Pending, Processing, Completed, Closed
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public DateTime? FinishedAt { get; set; }

        [NotMapped]
        public DateTime CreatedAt { get => CreateTime; set => CreateTime = value; }

        [NotMapped]
        public int? AlarmRecordId { get => AlarmId; set => AlarmId = value ?? 0; }
    }

    [Table("work_order_logs")]
    public class WorkOrderLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int WorkOrderId { get; set; }

        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        public int OperatorId { get; set; }

        [ForeignKey(nameof(OperatorId))]
        public SysUser? Operator { get; set; }

        [MaxLength(50)]
        public string OperatorName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PreviousStatus { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NewStatus { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ActionNote { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        [NotMapped]
        public DateTime CreatedAt { get => CreateTime; set => CreateTime = value; }
    }
}
