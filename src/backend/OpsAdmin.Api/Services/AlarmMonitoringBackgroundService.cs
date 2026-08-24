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

                // 每 20 秒执行一次轮询检测
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        private async Task EvaluateRulesAndGenerateAlarmsAsync(OpsDbContext db)
        {
            var rules = await db.AlarmRules.Where(r => r.IsEnabled).ToListAsync();
            var assets = await db.Assets.ToListAsync();

            var random = new Random();

            foreach (var asset in assets)
            {
                // 模拟指标的微幅起伏
                if (asset.Status == "Online")
                {
                    var cpuDelta = (random.NextDouble() - 0.48) * 3.0; // [-1.44, +1.56]
                    var memDelta = (random.NextDouble() - 0.48) * 2.0;
                    asset.CpuUsage = Math.Clamp(Math.Round(asset.CpuUsage + cpuDelta, 1), 5.0, 99.9);
                    asset.MemoryUsage = Math.Clamp(Math.Round(asset.MemoryUsage + memDelta, 1), 10.0, 99.0);
                    asset.UpdatedAt = DateTime.Now;
                }

                // 规则判定
                foreach (var rule in rules)
                {
                    bool triggered = false;
                    string alarmMsg = "";

                    if (rule.MetricType == "CpuUsage" && EvaluateCondition(asset.CpuUsage, rule.Operator, rule.Threshold))
                    {
                        triggered = true;
                        alarmMsg = $"资产 [{asset.Name}] (IP: {asset.IpAddress}) CPU 使用率到达 {asset.CpuUsage}%，触发规则 [{rule.Name}]";
                    }
                    else if (rule.MetricType == "MemoryUsage" && EvaluateCondition(asset.MemoryUsage, rule.Operator, rule.Threshold))
                    {
                        triggered = true;
                        alarmMsg = $"资产 [{asset.Name}] (IP: {asset.IpAddress}) 内存使用率到达 {asset.MemoryUsage}%，触发规则 [{rule.Name}]";
                    }
                    else if (rule.MetricType == "DiskUsage" && EvaluateCondition(asset.DiskUsage, rule.Operator, rule.Threshold))
                    {
                        triggered = true;
                        alarmMsg = $"资产 [{asset.Name}] (IP: {asset.IpAddress}) 磁盘使用率到达 {asset.DiskUsage}%，触发规则 [{rule.Name}]";
                    }
                    else if (rule.MetricType == "Offline" && asset.Status != "Online")
                    {
                        triggered = true;
                        alarmMsg = $"资产 [{asset.Name}] (IP: {asset.IpAddress}) 运行状态变为 {asset.Status}，触发脱机中断告警";
                    }

                    if (triggered)
                    {
                        // 检查 10 分钟内是否有同资产同规则的未处置告警，避免重复频繁发报警
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

        private bool EvaluateCondition(double val, string op, double threshold)
        {
            return op switch
            {
                ">" => val > threshold,
                ">=" => val >= threshold,
                "==" => Math.Abs(val - threshold) < 0.01,
                "<=" => val <= threshold,
                "<" => val < threshold,
                _ => val > threshold
            };
        }
    }
}
