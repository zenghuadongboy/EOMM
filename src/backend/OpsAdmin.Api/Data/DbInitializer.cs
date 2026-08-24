using System.Security.Cryptography;
using System.Text;
using OpsAdmin.Api.Entities;

namespace OpsAdmin.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(OpsDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // 1. Roles
            var adminRole = new SysRole { Code = "Admin", Name = "超级管理员", Description = "拥有系统全量控制权限与系统配置功能" };
            var opsRole = new SysRole { Code = "OpsStaff", Name = "运维工程师", Description = "具备设备资产维保、告警处置、监控指标查看权限" };
            var viewerRole = new SysRole { Code = "Viewer", Name = "只读访客", Description = "仅具备看板与日志查看权限，无法修改配置" };

            context.Roles.AddRange(adminRole, opsRole, viewerRole);
            context.SaveChanges();

            // 2. Permissions
            var permissions = new List<SysPermission>
            {
                new SysPermission { Code = "dashboard:view", Name = "运维大屏查看", Type = "Menu", Path = "/dashboard", SortOrder = 1 },
                new SysPermission { Code = "asset:view", Name = "资产列表查看", Type = "Menu", Path = "/assets", SortOrder = 2 },
                new SysPermission { Code = "asset:edit", Name = "资产新增修改", Type = "Button", Path = "", SortOrder = 3 },
                new SysPermission { Code = "asset:delete", Name = "资产删除", Type = "Button", Path = "", SortOrder = 4 },
                new SysPermission { Code = "alarm:view", Name = "告警中心查看", Type = "Menu", Path = "/alarms", SortOrder = 5 },
                new SysPermission { Code = "alarm:process", Name = "告警闭环处置", Type = "Button", Path = "", SortOrder = 6 },
                new SysPermission { Code = "alarm:rule", Name = "告警规则配置", Type = "Menu", Path = "/alarms/rules", SortOrder = 7 },
                new SysPermission { Code = "system:user", Name = "用户账号管理", Type = "Menu", Path = "/system/users", SortOrder = 8 },
                new SysPermission { Code = "system:role", Name = "角色权限管理", Type = "Menu", Path = "/system/roles", SortOrder = 9 },
                new SysPermission { Code = "system:log", Name = "日志审计中心", Type = "Menu", Path = "/system/logs", SortOrder = 10 }
            };
            context.Permissions.AddRange(permissions);
            context.SaveChanges();

            // Assign All Permissions to Admin
            foreach (var perm in permissions)
            {
                context.RolePermissions.Add(new SysRolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
            }

            // Assign subset to OpsStaff
            foreach (var perm in permissions.Where(p => p.Code != "system:user" && p.Code != "system:role"))
            {
                context.RolePermissions.Add(new SysRolePermission { RoleId = opsRole.Id, PermissionId = perm.Id });
            }

            // Assign view permissions to Viewer
            foreach (var perm in permissions.Where(p => p.Type == "Menu" && !p.Code.StartsWith("system:")))
            {
                context.RolePermissions.Add(new SysRolePermission { RoleId = viewerRole.Id, PermissionId = perm.Id });
            }
            context.SaveChanges();

            // 3. Users
            var users = new List<SysUser>
            {
                new SysUser
                {
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"),
                    RealName = "张管理员",
                    Email = "admin@company.com",
                    Phone = "13800000001",
                    Department = "IT运维部",
                    RoleId = adminRole.Id,
                    IsEnabled = true,
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    LastLoginAt = DateTime.Now
                },
                new SysUser
                {
                    Username = "ops",
                    PasswordHash = HashPassword("ops123"),
                    RealName = "李运维",
                    Email = "ops@company.com",
                    Phone = "13800000002",
                    Department = "基础架构组",
                    RoleId = opsRole.Id,
                    IsEnabled = true,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    LastLoginAt = DateTime.Now.AddHours(-2)
                },
                new SysUser
                {
                    Username = "viewer",
                    PasswordHash = HashPassword("viewer123"),
                    RealName = "王访客",
                    Email = "viewer@company.com",
                    Phone = "13800000003",
                    Department = "审计合规组",
                    RoleId = viewerRole.Id,
                    IsEnabled = true,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    LastLoginAt = DateTime.Now.AddDays(-1)
                }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // 4. Assets
            var assets = new List<Asset>
            {
                new Asset { AssetNo = "AST-SRV-001", Name = "Core-DB-01 核心业务主数据库", Type = "Database", IpAddress = "192.168.10.11", Port = 3306, Department = "数据中心", Manager = "李运维", Status = "Online", HardwareSpec = "64C 256GB NVMe 2TB", CpuUsage = 42.5, MemoryUsage = 78.2, DiskUsage = 65.0, PurchaseDate = DateTime.Now.AddYears(-2), MaintenanceExpire = DateTime.Now.AddYears(1) },
                new Asset { AssetNo = "AST-SRV-002", Name = "Core-DB-02 核心业务从数据库", Type = "Database", IpAddress = "192.168.10.12", Port = 3306, Department = "数据中心", Manager = "李运维", Status = "Online", HardwareSpec = "64C 256GB NVMe 2TB", CpuUsage = 38.1, MemoryUsage = 71.4, DiskUsage = 64.5, PurchaseDate = DateTime.Now.AddYears(-2), MaintenanceExpire = DateTime.Now.AddYears(1) },
                new Asset { AssetNo = "AST-SRV-003", Name = "Web-App-Node-01 前置网关", Type = "Server", IpAddress = "192.168.10.21", Port = 8080, Department = "研发部", Manager = "张管理员", Status = "Online", HardwareSpec = "16C 64GB SSD 500GB", CpuUsage = 88.6, MemoryUsage = 82.0, DiskUsage = 41.2, PurchaseDate = DateTime.Now.AddYears(-1), MaintenanceExpire = DateTime.Now.AddYears(2) },
                new Asset { AssetNo = "AST-SRV-004", Name = "Web-App-Node-02 前置网关", Type = "Server", IpAddress = "192.168.10.22", Port = 8080, Department = "研发部", Manager = "张管理员", Status = "Online", HardwareSpec = "16C 64GB SSD 500GB", CpuUsage = 24.3, MemoryUsage = 45.6, DiskUsage = 39.8, PurchaseDate = DateTime.Now.AddYears(-1), MaintenanceExpire = DateTime.Now.AddYears(2) },
                new Asset { AssetNo = "AST-CLS-005", Name = "Cloud-K8s-Worker-01 容器节点", Type = "CloudHost", IpAddress = "10.0.1.101", Port = 22, Department = "云平台组", Manager = "李运维", Status = "Online", HardwareSpec = "32C 128GB SSD 1TB", CpuUsage = 92.4, MemoryUsage = 91.5, DiskUsage = 88.0, PurchaseDate = DateTime.Now.AddMonths(-8), MaintenanceExpire = DateTime.Now.AddYears(2) },
                new Asset { AssetNo = "AST-NET-001", Name = "Core-SW-A 核心交换机", Type = "NetworkDevice", IpAddress = "192.168.1.1", Port = 23, Department = "网络工程组", Manager = "李运维", Status = "Online", HardwareSpec = "Cisco Catalyst 9500 48P", CpuUsage = 15.2, MemoryUsage = 32.0, DiskUsage = 12.0, PurchaseDate = DateTime.Now.AddYears(-3), MaintenanceExpire = DateTime.Now.AddMonths(2) },
                new Asset { AssetNo = "AST-NET-002", Name = "Edge-FW-01 出口主防火墙", Type = "NetworkDevice", IpAddress = "192.168.1.254", Port = 443, Department = "网络安全组", Manager = "李运维", Status = "Online", HardwareSpec = "Fortinet FortiGate 200F", CpuUsage = 58.7, MemoryUsage = 64.1, DiskUsage = 22.5, PurchaseDate = DateTime.Now.AddYears(-2), MaintenanceExpire = DateTime.Now.AddYears(1) },
                new Asset { AssetNo = "AST-STO-001", Name = "SAN-Storage-Primary 企业级存储阵列", Type = "Storage", IpAddress = "192.168.20.10", Port = 8443, Department = "数据中心", Manager = "李运维", Status = "Online", HardwareSpec = "Dell PowerStore 500T 100TB", CpuUsage = 18.0, MemoryUsage = 40.0, DiskUsage = 94.2, PurchaseDate = DateTime.Now.AddYears(-1), MaintenanceExpire = DateTime.Now.AddYears(4) },
                new Asset { AssetNo = "AST-SRV-006", Name = "Auth-OAuth2-Service 统一认证中心", Type = "Server", IpAddress = "192.168.10.50", Port = 5000, Department = "安全运维部", Manager = "张管理员", Status = "Fault", HardwareSpec = "8C 32GB SSD 250GB", CpuUsage = 0.0, MemoryUsage = 0.0, DiskUsage = 55.0, PurchaseDate = DateTime.Now.AddYears(-2), MaintenanceExpire = DateTime.Now.AddYears(1) },
                new Asset { AssetNo = "AST-NET-003", Name = "Backup-Router-B 备用路由网关", Type = "NetworkDevice", IpAddress = "192.168.1.2", Port = 22, Department = "网络工程组", Manager = "李运维", Status = "Offline", HardwareSpec = "Huawei NetEngine AR6000", CpuUsage = 0.0, MemoryUsage = 0.0, DiskUsage = 10.0, PurchaseDate = DateTime.Now.AddYears(-4), MaintenanceExpire = DateTime.Now.AddDays(-15) },
                new Asset { AssetNo = "AST-CLS-006", Name = "Cloud-K8s-Worker-02 容器节点", Type = "CloudHost", IpAddress = "10.0.1.102", Port = 22, Department = "云平台组", Manager = "李运维", Status = "Maintenance", HardwareSpec = "32C 128GB SSD 1TB", CpuUsage = 0.0, MemoryUsage = 0.0, DiskUsage = 40.0, PurchaseDate = DateTime.Now.AddMonths(-8), MaintenanceExpire = DateTime.Now.AddYears(2) },
                new Asset { AssetNo = "AST-SRV-007", Name = "Ops-Monitor-Master 运维平台主节点", Type = "Server", IpAddress = "192.168.10.88", Port = 5001, Department = "IT运维部", Manager = "张管理员", Status = "Online", HardwareSpec = "16C 64GB NVMe 500GB", CpuUsage = 31.4, MemoryUsage = 52.8, DiskUsage = 28.6, PurchaseDate = DateTime.Now.AddMonths(-3), MaintenanceExpire = DateTime.Now.AddYears(3) }
            };
            context.Assets.AddRange(assets);
            context.SaveChanges();

            // 5. Alarm Rules
            var rules = new List<AlarmRule>
            {
                new AlarmRule { Name = "CPU 资源严重超载预警", MetricType = "CpuUsage", Operator = ">", Threshold = 85.0, Severity = "Critical", IntervalSeconds = 60, IsEnabled = true, Description = "当服务器或云主机 CPU 使用率超过 85% 触发高优先级警报" },
                new AlarmRule { Name = "内存资源高占用警告", MetricType = "MemoryUsage", Operator = ">", Threshold = 90.0, Severity = "Warning", IntervalSeconds = 120, IsEnabled = true, Description = "当主机内存占用率高于 90% 时触发告警通知" },
                new AlarmRule { Name = "磁盘空间即将耗尽警报", MetricType = "DiskUsage", Operator = ">", Threshold = 90.0, Severity = "Critical", IntervalSeconds = 300, IsEnabled = true, Description = "存储空间剩余不足 10% 时触发告警，需紧急清理磁盘" },
                new AlarmRule { Name = "设备脱机/掉线紧急告警", MetricType = "Offline", Operator = "==", Threshold = 1.0, Severity = "Critical", IntervalSeconds = 30, IsEnabled = true, Description = "监测设备状态不处于 Online 时立即上报中断警报" }
            };
            context.AlarmRules.AddRange(rules);
            context.SaveChanges();

            // 6. Alarm Records
            var k8sAsset = assets.First(a => a.AssetNo == "AST-CLS-005");
            var authAsset = assets.First(a => a.AssetNo == "AST-SRV-006");
            var sanAsset = assets.First(a => a.AssetNo == "AST-STO-001");
            var webNodeAsset = assets.First(a => a.AssetNo == "AST-SRV-003");

            var alarmRecords = new List<AlarmRecord>
            {
                new AlarmRecord
                {
                    AlarmNo = "ALM-" + DateTime.Now.AddHours(-18).ToString("yyyyMMdd-HHmmss") + "-01",
                    AssetId = k8sAsset.Id,
                    RuleId = rules[0].Id,
                    Content = $"资产 [{k8sAsset.Name}] CPU 负载为 {k8sAsset.CpuUsage}%，高于临界阈值 85.0%",
                    Severity = "Critical",
                    Status = "Pending",
                    TriggeredAt = DateTime.Now.AddHours(-18),
                    Handler = "",
                    ResolutionNotes = ""
                },
                new AlarmRecord
                {
                    AlarmNo = "ALM-" + DateTime.Now.AddHours(-12).ToString("yyyyMMdd-HHmmss") + "-02",
                    AssetId = authAsset.Id,
                    RuleId = rules[3].Id,
                    Content = $"资产 [{authAsset.Name}] 响应超时，判定设备状态为 Fault（异常/故障）",
                    Severity = "Critical",
                    Status = "Processing",
                    TriggeredAt = DateTime.Now.AddHours(-12),
                    Handler = "李运维",
                    ResolutionNotes = "已联系机房值班人员检查服务器电源与网线配置，正在排查心跳网卡"
                },
                new AlarmRecord
                {
                    AlarmNo = "ALM-" + DateTime.Now.AddHours(-8).ToString("yyyyMMdd-HHmmss") + "-03",
                    AssetId = sanAsset.Id,
                    RuleId = rules[2].Id,
                    Content = $"资产 [{sanAsset.Name}] 磁盘存储占用率为 {sanAsset.DiskUsage}%，达到紧急告警阈值 90.0%",
                    Severity = "Critical",
                    Status = "Resolved",
                    TriggeredAt = DateTime.Now.AddHours(-8),
                    ResolvedAt = DateTime.Now.AddHours(-2),
                    Handler = "李运维",
                    ResolutionNotes = "已清理全量历史快照与日志文件，挂载扩容 20TB 独立存储卷，指标恢复正常"
                },
                new AlarmRecord
                {
                    AlarmNo = "ALM-" + DateTime.Now.AddHours(-3).ToString("yyyyMMdd-HHmmss") + "-04",
                    AssetId = webNodeAsset.Id,
                    RuleId = rules[0].Id,
                    Content = $"资产 [{webNodeAsset.Name}] CPU 使用率为 {webNodeAsset.CpuUsage}% 触发预警",
                    Severity = "Warning",
                    Status = "Pending",
                    TriggeredAt = DateTime.Now.AddHours(-3),
                    Handler = "",
                    ResolutionNotes = ""
                }
            };
            context.AlarmRecords.AddRange(alarmRecords);
            context.SaveChanges();

            // 7. Audit & Operation Logs
            var logs = new List<OperationLog>
            {
                new OperationLog { Username = "admin", Module = "身份认证", Action = "用户登录", IpAddress = "127.0.0.1", RequestParam = "{ username: 'admin' }", Success = true, ExecutionTimeMs = 12, Description = "管理员登录成功", CreatedAt = DateTime.Now.AddMinutes(-40) },
                new OperationLog { Username = "admin", Module = "资产管理", Action = "新增资产", IpAddress = "127.0.0.1", RequestParam = "{ name: 'Ops-Monitor-Master' }", Success = true, ExecutionTimeMs = 45, Description = "新增运维节点 AST-SRV-007", CreatedAt = DateTime.Now.AddMinutes(-30) },
                new OperationLog { Username = "ops", Module = "告警管理", Action = "处置告警", IpAddress = "192.168.10.15", RequestParam = "{ alarmNo: 'ALM-...-03', status: 'Resolved' }", Success = true, ExecutionTimeMs = 28, Description = "完成 SAN-Storage 磁盘告警处置闭环", CreatedAt = DateTime.Now.AddHours(-2) }
            };
            context.OperationLogs.AddRange(logs);

            var systemLogs = new List<SystemLog>
            {
                new SystemLog { Level = "Info", Source = "AlarmMonitoringBackgroundService", Message = "告警引擎巡检完成：检测 12 台在线设备，触发 1 项告警条件", CreatedAt = DateTime.Now.AddMinutes(-5) },
                new SystemLog { Level = "Info", Source = "DbInitializer", Message = "数据库初始化成功，依赖数据播种完成", CreatedAt = DateTime.Now.AddMinutes(-60) }
            };
            context.SystemLogs.AddRange(systemLogs);

            context.SaveChanges();
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "OpsAdminSecretSaltKey2026"));
            return Convert.ToBase64String(bytes);
        }
    }
}
