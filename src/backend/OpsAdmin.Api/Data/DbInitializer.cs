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
                EnsureSystemPermissionExists(context);
                return; // 数据库已初始化
            }

            // 1. 播种设备类型 (asset_types)
            var assetTypes = new List<AssetType>
            {
                new AssetType { TypeCode = "Server", TypeName = "物理服务器", Description = "数据中心物理机与刀片服务器", CreateTime = DateTime.Now.AddMonths(-6) },
                new AssetType { TypeCode = "CloudHost", TypeName = "云主机", Description = "阿里云/腾讯云虚拟化云计算节点", CreateTime = DateTime.Now.AddMonths(-6) },
                new AssetType { TypeCode = "NetworkDevice", TypeName = "网络设备", Description = "核心交换机、路由器、防火墙设备", CreateTime = DateTime.Now.AddMonths(-6) },
                new AssetType { TypeCode = "Storage", TypeName = "存储设备", Description = "SAN/NAS 企业级存储阵列与磁盘柜", CreateTime = DateTime.Now.AddMonths(-6) },
                new AssetType { TypeCode = "Database", TypeName = "数据库实例", Description = "MySQL/PostgreSQL/Redis 数据库集群", CreateTime = DateTime.Now.AddMonths(-6) }
            };
            context.AssetTypes.AddRange(assetTypes);
            context.SaveChanges();

            // 2. 播种角色 (sys_roles)
            var adminRole = new SysRole { RoleCode = "Admin", RoleName = "超级管理员", Description = "拥有系统全量控制权限与系统配置功能", CreateTime = DateTime.Now.AddMonths(-6) };
            var opsRole = new SysRole { RoleCode = "OpsStaff", RoleName = "运维工程师", Description = "具备设备资产维保、告警处置、监控指标查看权限", CreateTime = DateTime.Now.AddMonths(-6) };
            var viewerRole = new SysRole { RoleCode = "Viewer", RoleName = "只读访客", Description = "仅具备看板与日志查看权限，无法修改配置", CreateTime = DateTime.Now.AddMonths(-6) };

            context.Roles.AddRange(adminRole, opsRole, viewerRole);
            context.SaveChanges();

            // 3. 播种权限 (sys_permissions)
            var permissions = new List<SysPermission>
            {
                new SysPermission { PermissionCode = "dashboard:view", PermissionName = "运维大屏查看", Path = "/dashboard", Method = "GET", Description = "查看大屏概览数据", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "asset:view", PermissionName = "资产列表查看", Path = "/assets", Method = "GET", Description = "查看资产列表及详情", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "asset:edit", PermissionName = "资产新增修改", Path = "/assets/edit", Method = "POST", Description = "编辑资产信息", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "asset:delete", PermissionName = "资产删除", Path = "/assets/delete", Method = "DELETE", Description = "删除资产台账", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "alarm:view", PermissionName = "告警中心查看", Path = "/alarms", Method = "GET", Description = "查看告警记录列表", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "alarm:process", PermissionName = "告警闭环处置", Path = "/alarms/process", Method = "POST", Description = "处理并关单告警", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "alarm:rule", PermissionName = "告警规则配置", Path = "/alarms/rules", Method = "GET", Description = "查看与调整规则", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "system:user", PermissionName = "用户账号管理", Path = "/system/users", Method = "GET", Description = "管理系统用户", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "system:role", PermissionName = "角色权限管理", Path = "/system/roles", Method = "GET", Description = "配置角色与权限", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "system:permission", PermissionName = "权限节点管理", Path = "/system/permissions", Method = "GET", Description = "配置与维护功能节点", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "system:log", PermissionName = "日志审计中心", Path = "/system/logs", Method = "GET", Description = "查看审计日志", CreateTime = DateTime.Now }
            };
            context.Permissions.AddRange(permissions);
            context.SaveChanges();

            // 角色权限关联
            foreach (var perm in permissions)
            {
                context.RolePermissions.Add(new SysRolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
            }
            foreach (var perm in permissions.Where(p => p.PermissionCode != "system:user" && p.PermissionCode != "system:role"))
            {
                context.RolePermissions.Add(new SysRolePermission { RoleId = opsRole.Id, PermissionId = perm.Id });
            }
            foreach (var perm in permissions.Where(p => p.PermissionCode.Contains(":view")))
            {
                context.RolePermissions.Add(new SysRolePermission { RoleId = viewerRole.Id, PermissionId = perm.Id });
            }
            context.SaveChanges();

            // 4. 播种用户 (sys_users)
            var adminUser = new SysUser
            {
                UserName = "admin",
                PasswordHash = HashPassword("admin123"),
                RealName = "张管理员",
                Email = "admin@company.com",
                Status = "Enable",
                CreateTime = DateTime.Now.AddMonths(-6)
            };
            var opsUser = new SysUser
            {
                UserName = "ops",
                PasswordHash = HashPassword("ops123"),
                RealName = "李运维",
                Email = "ops@company.com",
                Status = "Enable",
                CreateTime = DateTime.Now.AddMonths(-3)
            };
            var viewerUser = new SysUser
            {
                UserName = "viewer",
                PasswordHash = HashPassword("viewer123"),
                RealName = "王访客",
                Email = "viewer@company.com",
                Status = "Enable",
                CreateTime = DateTime.Now.AddMonths(-1)
            };

            context.Users.AddRange(adminUser, opsUser, viewerUser);
            context.SaveChanges();

            // 播种用户角色关联 (sys_user_roles)
            context.UserRoles.AddRange(
                new SysUserRole { UserId = adminUser.Id, RoleId = adminRole.Id, CreateTime = DateTime.Now.AddMonths(-6) },
                new SysUserRole { UserId = opsUser.Id, RoleId = opsRole.Id, CreateTime = DateTime.Now.AddMonths(-3) },
                new SysUserRole { UserId = viewerUser.Id, RoleId = viewerRole.Id, CreateTime = DateTime.Now.AddMonths(-1) }
            );
            context.SaveChanges();

            // 5. 播种设备资产 (assets)
            var dbType = assetTypes.First(t => t.TypeCode == "Database");
            var serverType = assetTypes.First(t => t.TypeCode == "Server");
            var cloudType = assetTypes.First(t => t.TypeCode == "CloudHost");
            var netType = assetTypes.First(t => t.TypeCode == "NetworkDevice");
            var storageType = assetTypes.First(t => t.TypeCode == "Storage");

            var assets = new List<Asset>
            {
                new Asset { AssetNo = "AST-SRV-001", AssetName = "Core-DB-01 核心业务主数据库", AssetTypeId = dbType.Id, IpAddress = "192.168.10.11", Location = "A机房-机架A01", Status = "Online", Description = "MySQL 8.0 主节点，承载核心交易数据", CreateTime = DateTime.Now.AddYears(-2) },
                new Asset { AssetNo = "AST-SRV-002", AssetName = "Core-DB-02 核心业务从数据库", AssetTypeId = dbType.Id, IpAddress = "192.168.10.12", Location = "A机房-机架A02", Status = "Online", Description = "MySQL 8.0 从节点，读写分离同步中", CreateTime = DateTime.Now.AddYears(-2) },
                new Asset { AssetNo = "AST-SRV-003", AssetName = "Web-App-Node-01 前置网关", AssetTypeId = serverType.Id, IpAddress = "192.168.10.21", Location = "B机房-机架B05", Status = "Online", Description = "Nginx + Gateway 网关服务节点 1", CreateTime = DateTime.Now.AddYears(-1) },
                new Asset { AssetNo = "AST-CLS-005", AssetName = "Cloud-K8s-Worker-01 容器节点", AssetTypeId = cloudType.Id, IpAddress = "10.0.1.101", Location = "华东1可用区A", Status = "Online", Description = "K8s 工作节点，运行核心微服务容器", CreateTime = DateTime.Now.AddMonths(-8) },
                new Asset { AssetNo = "AST-NET-001", AssetName = "Core-SW-A 核心交换机", AssetTypeId = netType.Id, IpAddress = "192.168.1.1", Location = "网络核心机房-Rack01", Status = "Online", Description = "Cisco Catalyst 9500 万兆核心交换机", CreateTime = DateTime.Now.AddYears(-3) },
                new Asset { AssetNo = "AST-STO-001", AssetName = "SAN-Storage-Primary 企业级存储阵列", AssetTypeId = storageType.Id, IpAddress = "192.168.20.10", Location = "数据中心-S01柜", Status = "Online", Description = "Dell PowerStore 500T 100TB 光纤存储", CreateTime = DateTime.Now.AddYears(-1) },
                new Asset { AssetNo = "AST-SRV-006", AssetName = "Auth-OAuth2-Service 统一认证中心", AssetTypeId = serverType.Id, IpAddress = "192.168.10.50", Location = "A机房-机架A08", Status = "Fault", Description = "OAuth2/JWT 鉴权服务主机", CreateTime = DateTime.Now.AddYears(-2) },
                new Asset { AssetNo = "AST-NET-003", AssetName = "Backup-Router-B 备用路由网关", AssetTypeId = netType.Id, IpAddress = "192.168.1.2", Location = "备用机房-R02柜", Status = "Offline", Description = "华为 NetEngine 路由网关", CreateTime = DateTime.Now.AddYears(-4) }
            };
            context.Assets.AddRange(assets);
            context.SaveChanges();

            // 6. 播种告警规则 (alarm_roles)
            var alarmRules = new List<AlarmRule>
            {
                new AlarmRule { RuleName = "CPU 资源严重超载预警", AssetTypeId = serverType.Id, Metric = "CpuUsage", Threshold = "85", Severity = "Critical", Status = "Enable", Description = "当服务器 CPU 使用率超过 85% 触发警报", CreateTime = DateTime.Now },
                new AlarmRule { RuleName = "内存高占用警告", AssetTypeId = cloudType.Id, Metric = "MemoryUsage", Threshold = "90", Severity = "Warning", Status = "Enable", Description = "当云主机内存占用率高于 90% 时触发告警", CreateTime = DateTime.Now },
                new AlarmRule { RuleName = "磁盘存储即将耗尽警报", AssetTypeId = storageType.Id, Metric = "DiskUsage", Threshold = "90", Severity = "Critical", Status = "Enable", Description = "存储空间剩余不足 10% 时触发告警", CreateTime = DateTime.Now },
                new AlarmRule { RuleName = "设备离线紧急告警", AssetTypeId = netType.Id, Metric = "Offline", Threshold = "1", Severity = "Critical", Status = "Enable", Description = "网络设备离线中断告警", CreateTime = DateTime.Now }
            };
            context.AlarmRules.AddRange(alarmRules);
            context.SaveChanges();

            // 7. 播种告警记录 (alarm_records)
            var k8sAsset = assets.First(a => a.AssetNo == "AST-CLS-005");
            var authAsset = assets.First(a => a.AssetNo == "AST-SRV-006");
            var sanAsset = assets.First(a => a.AssetNo == "AST-STO-001");

            var alarmRecords = new List<AlarmRecord>
            {
                new AlarmRecord
                {
                    AlarmNo = "ALM-" + DateTime.Now.AddHours(-18).ToString("yyyyMMdd-HHmmss") + "-01",
                    AssetId = k8sAsset.Id,
                    RuleId = alarmRules[0].Id,
                    Content = $"设备 [{k8sAsset.AssetName}] CPU 负载持续高于阈值 85%",
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
                    RuleId = alarmRules[3].Id,
                    Content = $"设备 [{authAsset.AssetName}] 网络心跳超时，状态判定为 Fault",
                    Severity = "Critical",
                    Status = "Processing",
                    TriggeredAt = DateTime.Now.AddHours(-12),
                    Handler = "李运维",
                    ResolutionNotes = "已联系机房值班人员检查服务器电源与网线配置"
                },
                new AlarmRecord
                {
                    AlarmNo = "ALM-" + DateTime.Now.AddHours(-8).ToString("yyyyMMdd-HHmmss") + "-03",
                    AssetId = sanAsset.Id,
                    RuleId = alarmRules[2].Id,
                    Content = $"设备 [{sanAsset.AssetName}] 存储使用率超过 90%",
                    Severity = "Critical",
                    Status = "Resolved",
                    TriggeredAt = DateTime.Now.AddHours(-8),
                    ResolvedAt = DateTime.Now.AddHours(-2),
                    Handler = "李运维",
                    ResolutionNotes = "已清理快照与历史日志，挂载扩容 20TB 独立存储卷"
                }
            };
            context.AlarmRecords.AddRange(alarmRecords);
            context.SaveChanges();

            // 8. 播种工单与工单日志 (work_orders, work_order_logs)
            var workOrder = new WorkOrder
            {
                WorkOrderNo = "WO-" + DateTime.Now.AddHours(-10).ToString("yyyyMMdd-HHmmss") + "-01",
                Source = "AlarmTriggered",
                Priority = "High",
                AssetId = authAsset.Id,
                AlarmId = alarmRecords[1].Id,
                Handler = "李运维",
                Status = "Processing",
                Title = $"紧急修复 - {authAsset.AssetName} 离线故障",
                Description = "排查 Auth 鉴权服务器响应超时与关机异常",
                CreateTime = DateTime.Now.AddHours(-10)
            };
            context.WorkOrders.Add(workOrder);
            context.SaveChanges();

            var workOrderLog = new WorkOrderLog
            {
                WorkOrderId = workOrder.Id,
                OperatorId = opsUser.Id,
                OperatorName = opsUser.RealName,
                PreviousStatus = "Pending",
                NewStatus = "Processing",
                ActionNote = "已领单开始现场排查",
                CreateTime = DateTime.Now.AddHours(-9)
            };
            context.WorkOrderLogs.Add(workOrderLog);

            // 9. 播种日志
            var logs = new List<OperationLog>
            {
                new OperationLog { Username = "admin", Module = "身份认证", Action = "用户登录", IpAddress = "127.0.0.1", RequestParam = "{ username: 'admin' }", Success = true, ExecutionTimeMs = 12, Description = "管理员登录成功", CreatedAt = DateTime.Now.AddMinutes(-40) },
                new OperationLog { Username = "ops", Module = "告警管理", Action = "处置告警", IpAddress = "192.168.10.15", RequestParam = "{ status: 'Resolved' }", Success = true, ExecutionTimeMs = 28, Description = "完成存储告警处置", CreatedAt = DateTime.Now.AddHours(-2) }
            };
            context.OperationLogs.AddRange(logs);

            var systemLogs = new List<SystemLog>
            {
                new SystemLog { Level = "Info", Source = "DbInitializer", Message = "数据库严格按照 ER 图要求重新初始化成功", CreatedAt = DateTime.Now.AddMinutes(-5) }
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

        private static void EnsureSystemPermissionExists(OpsDbContext context)
        {
            var requiredPerms = new List<SysPermission>
            {
                new SysPermission { PermissionCode = "system:permission", PermissionName = "权限节点管理", Path = "/system/permissions", Method = "GET", Description = "配置与维护功能节点", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "monitoring:view", PermissionName = "设备监控管理", Path = "/monitoring", Method = "GET", Description = "设备实时状态与性能指标监控", CreateTime = DateTime.Now },
                new SysPermission { PermissionCode = "system:setting", PermissionName = "系统管理设置", Path = "/system/settings", Method = "GET", Description = "系统基础配置与参数管理", CreateTime = DateTime.Now }
            };

            foreach (var item in requiredPerms)
            {
                var perm = context.Permissions.FirstOrDefault(p => p.PermissionCode == item.PermissionCode);
                if (perm == null)
                {
                    perm = item;
                    context.Permissions.Add(perm);
                    context.SaveChanges();
                }

                var adminRole = context.Roles.FirstOrDefault(r => r.RoleCode == "Admin");
                if (adminRole != null)
                {
                    var hasRel = context.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == perm.Id);
                    if (!hasRel)
                    {
                        context.RolePermissions.Add(new SysRolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
                        context.SaveChanges();
                    }
                }
            }
        }
    }
}
