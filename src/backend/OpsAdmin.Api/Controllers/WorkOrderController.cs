using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsAdmin.Api.Data;
using OpsAdmin.Api.Entities;

namespace OpsAdmin.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkOrderController : ControllerBase
    {
        private readonly OpsDbContext _db;

        public WorkOrderController(OpsDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 分页多条件查询工单列表
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetList(
            [FromQuery] string? keyword,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _db.WorkOrders
                .Include(w => w.Asset)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(w => w.WorkOrderNo.Contains(keyword) || w.Title.Contains(keyword) || w.Handler.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(w => w.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(w => w.Priority == priority);
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { code = 200, message = "success", data = new { total, items, page, pageSize } });
        }

        /// <summary>
        /// 创建或编辑工单
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] WorkOrder model)
        {
            if (model.AssetId <= 0)
            {
                return BadRequest(new { code = 400, message = "关联设备AssetId必须大于0" });
            }

            var assetExists = await _db.Assets.AnyAsync(a => a.Id == model.AssetId);
            if (!assetExists)
            {
                return BadRequest(new { code = 400, message = "指定的关联设备不存在" });
            }

            WorkOrder? order;
            if (model.Id <= 0)
            {
                // 新建工单
                order = new WorkOrder
                {
                    WorkOrderNo = "WO" + DateTime.Now.ToString("yyyyMMddHHmmss") + Random.Shared.Next(100, 999),
                    Title = string.IsNullOrWhiteSpace(model.Title) ? "例行设备巡检与维护工单" : model.Title,
                    AssetId = model.AssetId,
                    AlarmRecordId = model.AlarmRecordId,
                    Source = string.IsNullOrWhiteSpace(model.Source) ? "Manual" : model.Source,
                    Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Medium" : model.Priority,
                    Status = "Pending",
                    Creator = User.Identity?.Name ?? "admin",
                    Handler = string.IsNullOrWhiteSpace(model.Handler) ? "admin" : model.Handler,
                    Description = model.Description ?? "",
                    CreatedAt = DateTime.Now
                };

                _db.WorkOrders.Add(order);
                await _db.SaveChangesAsync();

                // 记录初始日志
                _db.WorkOrderLogs.Add(new WorkOrderLog
                {
                    WorkOrderId = order.Id,
                    OperatorName = User.Identity?.Name ?? "admin",
                    PreviousStatus = "None",
                    NewStatus = "Pending",
                    ActionNote = $"工单创建成功，指派给负责人 [{order.Handler}]",
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return Ok(new { code = 200, message = "工单创建成功", data = order });
            }
            else
            {
                // 修改工单基本属性
                order = await _db.WorkOrders.FindAsync(model.Id);
                if (order == null) return NotFound(new { code = 444, message = "工单不存在" });

                order.Title = model.Title;
                order.Priority = model.Priority;
                order.Handler = model.Handler;
                order.Description = model.Description;

                await _db.SaveChangesAsync();
                return Ok(new { code = 200, message = "工单更新成功", data = order });
            }
        }

        /// <summary>
        /// 由告警记录一键生成运维处置工单
        /// </summary>
        [HttpPost("create-from-alarm")]
        public async Task<IActionResult> CreateFromAlarm([FromBody] CreateFromAlarmDto dto)
        {
            var alarm = await _db.AlarmRecords.Include(a => a.Asset).FirstOrDefaultAsync(a => a.Id == dto.AlarmRecordId);
            if (alarm == null) return NotFound(new { code = 444, message = "告警记录不存在" });

            // 查重：避免重复针对同一告警创单
            var existingWo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.AlarmRecordId == alarm.Id && w.Status != "Closed");
            if (existingWo != null)
            {
                return Ok(new { code = 200, message = "该告警已关联工单: " + existingWo.WorkOrderNo, data = existingWo });
            }

            var wo = new WorkOrder
            {
                WorkOrderNo = "WO" + DateTime.Now.ToString("yyyyMMddHHmmss") + Random.Shared.Next(100, 999),
                Title = $"[告警处置工单] {alarm.Asset?.Name} - {alarm.Severity}级别告警处置",
                AssetId = alarm.AssetId,
                AlarmRecordId = alarm.Id,
                Source = "AlarmTriggered",
                Priority = alarm.Severity == "Critical" ? "High" : "Medium",
                Status = "Pending",
                Creator = User.Identity?.Name ?? "admin",
                Handler = string.IsNullOrWhiteSpace(dto.Handler) ? "admin" : dto.Handler,
                Description = $"关联告警内容: {alarm.Content}。告警时间: {alarm.TriggeredAt:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = DateTime.Now
            };

            _db.WorkOrders.Add(wo);

            // 同时将告警记录状态变更为 Processing
            alarm.Status = "Processing";
            alarm.Handler = wo.Handler;

            await _db.SaveChangesAsync();

            // 日志
            _db.WorkOrderLogs.Add(new WorkOrderLog
            {
                WorkOrderId = wo.Id,
                OperatorName = User.Identity?.Name ?? "admin",
                PreviousStatus = "None",
                NewStatus = "Pending",
                ActionNote = $"由告警记录[{alarm.AlarmNo}]自动联动派发工单",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Ok(new { code = 200, message = "告警联动工单派发成功", data = wo });
        }

        /// <summary>
        /// 工单状态流转变更 (状态机)
        /// </summary>
        [HttpPost("change-status")]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeStatusDto dto)
        {
            var order = await _db.WorkOrders.FindAsync(dto.WorkOrderId);
            if (order == null) return NotFound(new { code = 444, message = "工单不存在" });

            var oldStatus = order.Status;
            order.Status = dto.NewStatus;
            if (!string.IsNullOrWhiteSpace(dto.ResolutionNotes))
            {
                order.ResolutionNotes = dto.ResolutionNotes;
            }

            if (dto.NewStatus == "Completed" || dto.NewStatus == "Closed")
            {
                order.FinishedAt = DateTime.Now;

                // 若关单或完成，联动将关联的告警记录更新为 Resolved
                if (order.AlarmRecordId.HasValue)
                {
                    var alarm = await _db.AlarmRecords.FindAsync(order.AlarmRecordId.Value);
                    if (alarm != null)
                    {
                        alarm.Status = "Resolved";
                        alarm.ResolvedAt = DateTime.Now;
                        alarm.ResolutionNotes = $"通过工单[{order.WorkOrderNo}]成功处置闭环: " + dto.ResolutionNotes;
                    }
                }
            }

            _db.WorkOrderLogs.Add(new WorkOrderLog
            {
                WorkOrderId = order.Id,
                OperatorName = User.Identity?.Name ?? "admin",
                PreviousStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ActionNote = string.IsNullOrWhiteSpace(dto.Note) ? $"状态变更: {oldStatus} -> {dto.NewStatus}" : dto.Note,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Ok(new { code = 200, message = $"工单状态更新为 {dto.NewStatus}", data = order });
        }

        /// <summary>
        /// 查询指定工单的流转履历日志
        /// </summary>
        [HttpGet("logs/{workOrderId}")]
        public async Task<IActionResult> GetLogs(int workOrderId)
        {
            var logs = await _db.WorkOrderLogs
                .Where(l => l.WorkOrderId == workOrderId)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            return Ok(new { code = 200, message = "success", data = logs });
        }
    }

    public class CreateFromAlarmDto
    {
        public int AlarmRecordId { get; set; }
        public string Handler { get; set; } = "admin";
    }

    public class ChangeStatusDto
    {
        public int WorkOrderId { get; set; }
        public string NewStatus { get; set; } = "Processing";
        public string Note { get; set; } = string.Empty;
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
