using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsAdmin.Api.Data;
using OpsAdmin.Api.DTOs;
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
            var items = await query.OrderByDescending(w => w.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { total, items, page, pageSize }, "success"));
        }

        /// <summary>
        /// 创建或编辑工单
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] WorkOrder model)
        {
            if (model.AssetId <= 0)
            {
                return BadRequest(ApiResponse<string>.Fail("关联设备AssetId必须大于0"));
            }

            var assetExists = await _db.Assets.AnyAsync(a => a.Id == model.AssetId);
            if (!assetExists)
            {
                return BadRequest(ApiResponse<string>.Fail("指定的关联设备不存在"));
            }

            var currentUsername = User.Identity?.Name ?? "admin";
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == currentUsername);
            var operatorId = currentUser?.Id ?? 1;

            WorkOrder? order;
            if (model.Id <= 0)
            {
                order = new WorkOrder
                {
                    WorkOrderNo = "WO" + DateTime.Now.ToString("yyyyMMddHHmmss") + Random.Shared.Next(100, 999),
                    Title = string.IsNullOrWhiteSpace(model.Title) ? "例行设备巡检与维护工单" : model.Title,
                    AssetId = model.AssetId,
                    AlarmId = model.AlarmId > 0 ? model.AlarmId : (model.AlarmRecordId ?? 0),
                    Source = string.IsNullOrWhiteSpace(model.Source) ? "Manual" : model.Source,
                    Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Medium" : model.Priority,
                    Status = "Pending",
                    Handler = string.IsNullOrWhiteSpace(model.Handler) ? currentUsername : model.Handler,
                    Description = model.Description ?? "",
                    CreateTime = DateTime.Now
                };

                _db.WorkOrders.Add(order);
                await _db.SaveChangesAsync();

                _db.WorkOrderLogs.Add(new WorkOrderLog
                {
                    WorkOrderId = order.Id,
                    OperatorId = operatorId,
                    OperatorName = currentUser?.RealName ?? currentUsername,
                    PreviousStatus = "None",
                    NewStatus = "Pending",
                    ActionNote = $"工单创建成功，指派给负责人 [{order.Handler}]",
                    CreateTime = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return Ok(ApiResponse<WorkOrder>.Ok(order, "工单创建成功"));
            }
            else
            {
                order = await _db.WorkOrders.FindAsync(model.Id);
                if (order == null) return NotFound(ApiResponse<string>.Fail("工单不存在"));

                order.Title = model.Title;
                order.Priority = model.Priority;
                order.Handler = model.Handler;
                order.Description = model.Description;

                await _db.SaveChangesAsync();
                return Ok(ApiResponse<WorkOrder>.Ok(order, "工单更新成功"));
            }
        }

        /// <summary>
        /// 由告警记录一键生成运维处置工单
        /// </summary>
        [HttpPost("create-from-alarm")]
        public async Task<IActionResult> CreateFromAlarm([FromBody] CreateFromAlarmDto dto)
        {
            var alarm = await _db.AlarmRecords.Include(a => a.Asset).FirstOrDefaultAsync(a => a.Id == dto.AlarmRecordId);
            if (alarm == null) return NotFound(ApiResponse<string>.Fail("告警记录不存在"));

            var existingWo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.AlarmId == alarm.Id && w.Status != "Closed");
            if (existingWo != null)
            {
                return Ok(ApiResponse<WorkOrder>.Ok(existingWo, "该告警已关联工单: " + existingWo.WorkOrderNo));
            }

            var currentUsername = User.Identity?.Name ?? "admin";
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == currentUsername);
            var operatorId = currentUser?.Id ?? 1;

            var wo = new WorkOrder
            {
                WorkOrderNo = "WO" + DateTime.Now.ToString("yyyyMMddHHmmss") + Random.Shared.Next(100, 999),
                Title = $"[告警处置工单] {alarm.Asset?.AssetName} - {alarm.Severity}级别告警处置",
                AssetId = alarm.AssetId,
                AlarmId = alarm.Id,
                Source = "AlarmTriggered",
                Priority = alarm.Severity == "Critical" ? "High" : "Medium",
                Status = "Pending",
                Handler = string.IsNullOrWhiteSpace(dto.Handler) ? currentUsername : dto.Handler,
                Description = $"关联告警内容: {alarm.Content}。告警时间: {alarm.TriggeredAt:yyyy-MM-dd HH:mm:ss}",
                CreateTime = DateTime.Now
            };

            _db.WorkOrders.Add(wo);
            alarm.Status = "Processing";
            alarm.Handler = wo.Handler;

            await _db.SaveChangesAsync();

            _db.WorkOrderLogs.Add(new WorkOrderLog
            {
                WorkOrderId = wo.Id,
                OperatorId = operatorId,
                OperatorName = currentUser?.RealName ?? currentUsername,
                PreviousStatus = "None",
                NewStatus = "Pending",
                ActionNote = $"由告警记录[{alarm.AlarmNo}]自动联动派发工单",
                CreateTime = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<WorkOrder>.Ok(wo, "告警联动工单派发成功"));
        }

        /// <summary>
        /// 工单状态流转变更 (状态机)
        /// </summary>
        [HttpPost("change-status")]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeStatusDto dto)
        {
            var order = await _db.WorkOrders.FindAsync(dto.WorkOrderId);
            if (order == null) return NotFound(ApiResponse<string>.Fail("工单不存在"));

            var oldStatus = order.Status;
            order.Status = dto.NewStatus;

            var currentUsername = User.Identity?.Name ?? "admin";
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == currentUsername);
            var operatorId = currentUser?.Id ?? 1;

            if (dto.NewStatus == "Completed" || dto.NewStatus == "Closed")
            {
                order.FinishedAt = DateTime.Now;

                if (order.AlarmId > 0)
                {
                    var alarm = await _db.AlarmRecords.FindAsync(order.AlarmId);
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
                OperatorId = operatorId,
                OperatorName = currentUser?.RealName ?? currentUsername,
                PreviousStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ActionNote = string.IsNullOrWhiteSpace(dto.Note) ? $"状态变更: {oldStatus} -> {dto.NewStatus}" : dto.Note,
                CreateTime = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<WorkOrder>.Ok(order, $"工单状态更新为 {dto.NewStatus}"));
        }

        /// <summary>
        /// 查询指定工单的流转履历日志
        /// </summary>
        [HttpGet("logs/{workOrderId}")]
        public async Task<IActionResult> GetLogs(int workOrderId)
        {
            var logs = await _db.WorkOrderLogs
                .Where(l => l.WorkOrderId == workOrderId)
                .OrderBy(l => l.CreateTime)
                .ToListAsync();

            return Ok(ApiResponse<List<WorkOrderLog>>.Ok(logs, "success"));
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
