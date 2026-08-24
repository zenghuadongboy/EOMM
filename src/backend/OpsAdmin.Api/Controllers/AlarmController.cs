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
    public class AlarmController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public AlarmController(OpsDbContext context)
        {
            _context = context;
        }

        // --- 1. Alarm Records ---

        [HttpGet("records")]
        public async Task<ActionResult<ApiResponse<PagedResult<object>>>> GetRecords(
            [FromQuery] string? status,
            [FromQuery] string? severity,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var q = _context.AlarmRecords
                .Include(r => r.Asset)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(r => r.Status == status);
            }
            if (!string.IsNullOrWhiteSpace(severity))
            {
                q = q.Where(r => r.Severity == severity);
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(r => r.TriggeredAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.AlarmNo,
                    r.AssetId,
                    AssetName = r.Asset != null ? r.Asset.Name : "未知设备",
                    IpAddress = r.Asset != null ? r.Asset.IpAddress : "",
                    r.Content,
                    r.Severity,
                    r.Status,
                    r.TriggeredAt,
                    r.Handler,
                    r.ResolutionNotes,
                    r.ResolvedAt
                })
                .ToListAsync();

            var result = new PagedResult<object>
            {
                Items = items.Cast<object>().ToList(),
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<object>>.Ok(result));
        }

        [HttpPost("process")]
        public async Task<ActionResult<ApiResponse<string>>> ProcessAlarm([FromBody] ProcessAlarmRequest req)
        {
            var username = User.Identity?.Name ?? "system";
            var record = await _context.AlarmRecords.Include(r => r.Asset).FirstOrDefaultAsync(r => r.Id == req.AlarmId);
            if (record == null) return Ok(ApiResponse<string>.Fail("告警记录不存在"));

            record.Status = req.Status;
            record.Handler = username;
            record.ResolutionNotes = req.ResolutionNotes;
            if (req.Status == "Resolved")
            {
                record.ResolvedAt = DateTime.Now;
            }

            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
                Module = "告警管理",
                Action = "处置告警",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ alarmId: {req.AlarmId}, status: '{req.Status}' }}",
                Success = true,
                Description = $"用户处置告警 [{record.AlarmNo}] 状态更新为 {req.Status}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("告警处理记录更新成功"));
        }

        // --- 2. Alarm Rules ---

        [HttpGet("rules")]
        public async Task<ActionResult<ApiResponse<List<AlarmRule>>>> GetRules()
        {
            var rules = await _context.AlarmRules.OrderByDescending(r => r.Id).ToListAsync();
            return Ok(ApiResponse<List<AlarmRule>>.Ok(rules));
        }

        [HttpPost("rule/save")]
        public async Task<ActionResult<ApiResponse<AlarmRule>>> SaveRule([FromBody] AlarmRule rule)
        {
            var username = User.Identity?.Name ?? "system";

            if (rule.Id == 0)
            {
                _context.AlarmRules.Add(rule);
            }
            else
            {
                var existing = await _context.AlarmRules.FindAsync(rule.Id);
                if (existing == null) return Ok(ApiResponse<AlarmRule>.Fail("规则不存在"));

                existing.Name = rule.Name;
                existing.MetricType = rule.MetricType;
                existing.Operator = rule.Operator;
                existing.Threshold = rule.Threshold;
                existing.Severity = rule.Severity;
                existing.IntervalSeconds = rule.IntervalSeconds;
                existing.IsEnabled = rule.IsEnabled;
                existing.Description = rule.Description;
            }

            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
                Module = "告警策略",
                Action = "保存规则",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ name: '{rule.Name}' }}",
                Success = true,
                Description = $"修改或新增告警规则 [{rule.Name}]",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<AlarmRule>.Ok(rule, "告警规则保存成功"));
        }

        [HttpDelete("rule/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteRule(int id)
        {
            var rule = await _context.AlarmRules.FindAsync(id);
            if (rule == null) return Ok(ApiResponse<string>.Fail("规则不存在"));

            _context.AlarmRules.Remove(rule);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("告警规则移除成功"));
        }
    }
}
