using OpsAdmin.Api.Data;
using OpsAdmin.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace OpsAdmin.Api.Services
{
    public class AlarmMonitoringBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlarmMonitoringBackgroundService> _logger;

        public AlarmMonitoringBackgroundService(IServiceProvider serviceProvider, ILogger<AlarmMonitoringBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("告警自动化监测引擎启动...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<OpsDbContext>();
                        await EvaluateRulesAndGenerateAlarmsAsync(db);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "告警监测引擎运行时捕获异常");
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        private async Task EvaluateRulesAndGenerateAlarmsAsync(OpsDbContext db)
        {
            var rules = await db.AlarmRules.Where(r => r.Status == "Enable").ToListAsync();
            var assets = await db.Assets.Include(a => a.AssetType).ToListAsync();

            var random = new Random();

            foreach (var asset in assets)
            {
                // 根据关联类型匹配告警规则
                var matchedRules = rules.Where(r => r.AssetTypeId == asset.AssetTypeId || r.AssetTypeId == 0);

                foreach (var rule in matchedRules)
                {
                    bool triggered = false;
                    string alarmMsg = "";

                    if (rule.Metric == "Offline" && asset.Status != "Online")
                    {
                        triggered = true;
                        alarmMsg = $"资产 [{asset.AssetName}] (IP: {asset.IpAddress}) 运行状态变为 {asset.Status}，触发离线/故障告警";
                    }
                    else if (rule.Metric == "CpuUsage" && double.TryParse(rule.Threshold, out double threshold) && asset.Status != "Online")
                    {
                        triggered = true;
                        alarmMsg = $"资产 [{asset.AssetName}] (IP: {asset.IpAddress}) 触发规则 [{rule.RuleName}]";
                    }

                    if (triggered)
                    {
                        var recentRecord = await db.AlarmRecords
                            .Where(r => r.AssetId == asset.Id && r.RuleId == rule.Id && (r.Status == "Pending" || r.Status == "Processing"))
                            .OrderByDescending(r => r.TriggeredAt)
                            .FirstOrDefaultAsync();

                        if (recentRecord == null)
                        {
                            var newRecord = new AlarmRecord
                            {
                                AlarmNo = "ALM-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + random.Next(10, 99),
                                AssetId = asset.Id,
                                RuleId = rule.Id,
                                Content = alarmMsg,
                                Severity = rule.Severity,
                                Status = "Pending",
                                TriggeredAt = DateTime.Now
                            };

                            db.AlarmRecords.Add(newRecord);
                            db.SystemLogs.Add(new SystemLog
                            {
                                Level = rule.Severity == "Critical" ? "Error" : "Warn",
                                Source = "AlarmEngine",
                                Message = $"自动告警引擎生成新告警 #{newRecord.AlarmNo}: {alarmMsg}",
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
