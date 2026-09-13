using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsAdmin.Api.Data;
using OpsAdmin.Api.DTOs;

namespace OpsAdmin.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public DashboardController(OpsDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetOverview()
        {
            var totalAssets = await _context.Assets.CountAsync();
            var onlineAssets = await _context.Assets.CountAsync(a => a.Status == "Online");
            var faultAssets = await _context.Assets.CountAsync(a => a.Status == "Fault" || a.Status == "Offline");
            var pendingAlarms = await _context.AlarmRecords.CountAsync(r => r.Status == "Pending");

            // 按设备类型统计 (Join AssetType)
            var assetsByType = await _context.Assets
                .Include(a => a.AssetType)
                .GroupBy(a => a.AssetType != null ? a.AssetType.TypeName : "通用设备")
                .Select(g => new ChartItem
                {
                    Name = g.Key,
                    Value = g.Count()
                })
                .ToListAsync();

            // 按状态统计
            var assetsByStatus = await _context.Assets
                .GroupBy(a => a.Status)
                .Select(g => new ChartItem
                {
                    Name = g.Key == "Online" ? "在线" :
                           g.Key == "Offline" ? "离线" :
                           g.Key == "Fault" ? "故障" :
                           g.Key == "Maintenance" ? "维修中" : g.Key,
                    Value = g.Count()
                })
                .ToListAsync();

            // 按告警级别统计
            var alarmsBySeverity = await _context.AlarmRecords
                .GroupBy(r => r.Severity)
                .Select(g => new ChartItem
                {
                    Name = g.Key == "Critical" ? "紧急" :
                           g.Key == "Warning" ? "重要" : "一般",
                    Value = g.Count()
                })
                .ToListAsync();

            // 24H 告警趋势
            var trendPoints = new List<TrendPoint>();
            var now = DateTime.Now;

            for (int i = 5; i >= 0; i--)
            {
                var startHour = now.AddHours(-i * 4);
                var endHour = startHour.AddHours(4);
                var label = startHour.ToString("HH:00");

                var count = await _context.AlarmRecords
                    .CountAsync(r => r.TriggeredAt >= startHour && r.TriggeredAt < endHour);

                trendPoints.Add(new TrendPoint { TimeLabel = label, Count = count });
            }

            // 高频故障设备 Top5
            var topFaults = await _context.AlarmRecords
                .Include(r => r.Asset)
                .GroupBy(r => r.AssetId)
                .Select(g => new
                {
                    AssetId = g.Key,
                    Count = g.Count(),
                    AssetName = g.First().Asset != null ? g.First().Asset!.AssetName : "未知资产",
                    IpAddress = g.First().Asset != null ? g.First().Asset!.IpAddress : ""
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Select(x => new TopFaultAssetDto
                {
                    AssetName = x.AssetName,
                    IpAddress = x.IpAddress,
                    FaultCount = x.Count
                })
                .ToListAsync();

            // 平均修复耗时 MTTR
            var resolvedRecords = await _context.AlarmRecords
                .Where(r => r.Status == "Resolved" && r.ResolvedAt.HasValue)
                .ToListAsync();

            double avgMttrHours = 1.8;
            if (resolvedRecords.Any())
            {
                var totalHours = resolvedRecords.Sum(r => (r.ResolvedAt!.Value - r.TriggeredAt).TotalHours);
                avgMttrHours = Math.Round(totalHours / resolvedRecords.Count, 1);
            }

            var dto = new DashboardOverviewDto
            {
                TotalAssets = totalAssets,
                OnlineAssets = onlineAssets,
                FaultAssets = faultAssets,
                PendingAlarms = pendingAlarms,
                AverageCpuUsage = 45.2,
                AverageMemoryUsage = 62.8,
                AssetsByType = assetsByType,
                AssetsByStatus = assetsByStatus,
                AlarmsBySeverity = alarmsBySeverity,
                AlarmTrend24H = trendPoints,
                TopFaultAssets = topFaults,
                AverageMttrHours = avgMttrHours
            };

            return Ok(ApiResponse<DashboardOverviewDto>.Ok(dto));
        }
    }
}
