# 基于.NET的企业运维管理平台设计与实现 - 架构设计与开发实施计划

本实施方案 strictly 基于《毕业论文开题报告.txt》中定义的需求与架构设计规范，结合高级软件架构师经验，采用 **ASP.NET Core 8.0 Web API** 结合 **Vue 3 + Element Plus + ECharts** 技术栈，构建一套功能完备、高保真现代化UI、轻量级高性能的企业级 IT 运维管理平台。

---

## 1. 核心需求与设计目标

基于开题报告，系统包含以下 **五大核心业务模块**：
1. **用户与身份权限管理模块 (RBAC + JWT)**
   - 基于无状态 JWT (JSON Web Token) 的身份校验与 Refresh Token 刷新机制。
   - 基于 RBAC (Role-Based Access Control) 模型的细粒度权限管控（支持菜单级、按钮级与接口级过滤器校验）。
   - 角色定义（如：超级管理员、运维主管、基础运维员、只读审计员等）。
2. **设备资产全生命周期管理模块**
   - 资产台账：设备编号、名称、资产类型（物理服务器、云主机、网络设备、存储设备、数据库应用等）、IP地址、所属部门、责任人、硬件配置、购置/维保日期。
   - 运行状态监测：在线、离线、故障、维修中，以及动态指标（CPU%、内存%、磁盘%）。
   - 多条件组合筛选、批量数据导入/导出模拟、设备详情监控面板。
3. **告警自动化与闭环处理模块**
   - 告警规则引擎：支持按指标（离线、CPU/内存/磁盘超标）配置规则阈值、规则级别（一般、重要、紧急）。
   - 实时/定时检测后台服务：基于 ASP.NET Core `BackgroundService` 定期轮询检测设备状态，自动触发生成告警记录。
   - 告警闭环流转：待处理 -> 处理中 -> 已解决 -> 已忽略，记录处理历史与解决结果复盘。
   - 站内告警通知抽屉与实时提醒。
4. **日志与审计管理模块**
   - 操作日志：自动拦截并记录用户登录、资产增删改、权限配置、告警处置等敏感操作（不可篡改、包含IP与差异）。
   - 系统运行日志：记录系统接口调用、异常堆栈与后台定时任务触发日志。
5. **运维数据可视化分析大屏模块**
   - 包含数据概览卡片、资产类型/部门分布饼图与柱状图、设备实时在线率。
   - 告警级别分布、高频故障设备 Top5、平均故障修复时间 (MTTR) 计算与趋势折线图。

---

## 2. 系统总体架构与技术选型

```
+-----------------------------------------------------------------------+
|                         前端表示层 (Presentation)                     |
| Vue 3 (Composition API) + Vite + Element Plus + ECharts 5 + Pinia      |
+-----------------------------------------------------------------------+
                                   | HTTP / RESTful API (JSON)
                                   v
+-----------------------------------------------------------------------+
|                         后端接口层 (Web API Layer)                     |
| ASP.NET Core 8.0 Web API + JWT Middleware + Permission ActionFilter   |
+-----------------------------------------------------------------------+
                                   |
                                   v
+-----------------------------------------------------------------------+
|                        业务逻辑与后台服务 (Services)                   |
| UserService, AssetService, AlarmEngineService, LogService, Background  |
+-----------------------------------------------------------------------+
                                   |
                                   v
+-----------------------------------------------------------------------+
|                         数据持久层 (Data Access)                       |
| Entity Framework Core 8.0 (ORM) + SQLite / MySQL Database             |
+-----------------------------------------------------------------------+
```

### 技术栈规范：
- **后端**：ASP.NET Core 8.0, EF Core 8.0, JWT Authentication, System.Text.Json, BackgroundService 定时告警检测引擎。
- **前端**：Vue 3 (Script Setup), Vite, Pinia, Vue Router 4, Element Plus, ECharts, Axios, Lucide-Vue / Element Icons。
- **交互与外观**：支持暗黑/明亮现代模式切换，流线型卡片布局，防呆交互与丰富动画。

---

## 3. 数据库设计方案 (EF Core 实体)

拟创建 9 张核心关系数据库表：
1. `SysUser` - 系统用户表（包含 PasswordHash, Salt, RoleId, Department, Status）
2. `SysRole` - 角色表（Id, Code, Name, Description）
3. `SysPermission` - 权限表（Id, Code, Name, Type[Menu/Button/Api], Path, ParentId）
4. `SysRolePermission` - 角色与权限映射表
5. `Asset` - 资产表（Id, AssetNo, Name, Type, IpAddress, Port, Department, Manager, Status, CpuUsage, MemoryUsage, DiskUsage, MaintenanceExpire, HardwareSpec）
6. `AlarmRule` - 告警规则表（Id, Name, MetricType, Operator, Threshold, Severity[Info/Warning/Critical], IntervalSeconds, IsEnabled）
7. `AlarmRecord` - 告警记录表（Id, AlarmNo, AssetId, RuleId, Content, Severity, Status[Pending/Processing/Resolved/Ignored], TriggeredAt, Handler, ResolutionNotes, ResolvedAt）
8. `OperationLog` - 操作日志表（Id, Username, Module, Action, IpAddress, RequestParam, Status, ExecutionTimeMs, CreatedAt）
9. `SystemLog` - 系统运行日志表（Id, Level, Source, Message, ExceptionDetails, CreatedAt）

---

## 4. 拟创建的项目文件结构

```
d:\大学\基于.NET的企业运维管理平\
├── src/
│   ├── backend/
│   │   └── OpsAdmin.Api/
│   │       ├── Controllers/ (AuthController, UserController, RoleController, AssetController, AlarmController, LogController, DashboardController)
│   │       ├── Data/ (OpsDbContext.cs, DbInitializer.cs)
│   │       ├── Entities/ (SysUser.cs, SysRole.cs, Asset.cs, AlarmRule.cs, AlarmRecord.cs, OperationLog.cs...)
│   │       ├── DTOs/ (AuthDtos.cs, AssetDtos.cs, AlarmDtos.cs...)
│   │       ├── Services/ (IAuthService, IAssetService, AlarmMonitoringService...)
│   │       ├── Middleware/ (JwtMiddleware.cs, GlobalExceptionMiddleware.cs, AuditLogFilter.cs)
│   │       ├── Program.cs
│   │       └── OpsAdmin.Api.csproj
│   └── frontend/
│       ├── src/
│       │   ├── api/ (auth.js, asset.js, alarm.js, user.js, log.js, dashboard.js)
│       │   ├── assets/ (main.css, variables.css)
│       │   ├── components/ (Navbar.vue, Sidebar.vue, StatsCard.vue, AlarmNoticeDrawer.vue)
│       │   ├── views/
│       │   │   ├── login/Index.vue
│       │   │   ├── dashboard/Index.vue
│       │   │   ├── assets/List.vue
│       │   │   ├── alarms/List.vue
│       │   │   ├── alarms/Rules.vue
│       │   │   ├── system/Users.vue
│       │   │   ├── system/Roles.vue
│       │   │   └── system/Logs.vue
│       │   ├── router/index.js
│       │   ├── store/user.js
│       │   ├── App.vue
│       │   └── main.js
│       ├── package.json
│       └── vite.config.js
```

---

## 5. 详细开发与推进步骤

### 阶段一：后端 API 开发与数据库设计 (`src/backend/OpsAdmin.Api`)
1. 初始化 ASP.NET Core 8.0 Web API 项目与依赖配置（EF Core, JWT, Swagger/OpenAPI）。
2. 定义全量实体与 EF Core `OpsDbContext` 上下文。
3. 实现数据种子播种类 `DbInitializer`，初始化默认角色（管理员/运维员/只读审计员）、预置 10+ 真实模拟服务器与网络设备资产、告警规则及演示数据。
4. 开发 JWT 颁发/刷新机制与权限拦截器/AuditLog 审计过滤器。
5. 编写全套 API 控制器（Auth, Users, Roles, Assets, Alarms, Logs, Dashboard）。
6. 实现 `BackgroundService` 后台轮询服务 `AlarmMonitoringBackgroundService`，自动评估资产指标并实时生成告警。

### 阶段二：前端现代 UI 开发 (`src/frontend`)
1. 创建 Vue 3 + Vite 工程，配置 Axios 统一请求拦截、JWT Token 处理与 Element Plus / ECharts。
2. 实现全套 UI 模块：
   - 登录页面：支持炫酷动画、账号密码与验证码模拟。
   - 运维大屏 Dashboard：内置 4 大核心指标卡、资产状态占比饼图、设备在线率柱状图、24 小时告警趋势折线图、告警 Level 比例图及最新告警看板。
   - 资产管理控制台：多条件查询、资产新建/修改/查看、状态变更（在线/离线/维修）、模拟资源突增/模拟故障触发功能。
   - 告警中心：告警规则配置管理、告警记录流转处置（指派、填写解决方案、改变状态为已解决/忽略）。
   - 系统权限管理：用户列表（角色分配、禁用）、角色列表与权限分配树。
   - 审计日志中心：操作日志多维过滤查询、详情查看、运行日志看板。

### 阶段三：联调测试与功能验证
1. 启动 ASP.NET Core API 服务，前端通过代理联调。
2. 验证全流程：用户登录 -> 权限管控 -> 查看大屏 -> 增删资产 -> 模拟触发告警 -> 告警自动生成与站内通知 -> 处置告警闭环 -> 查看操作审计日志。
3. 编写完整说明文档与验证指南。

---

## 6. 验证计划

### 自动化与接口验证
- 运行 `dotnet run` 验证 API 服务的编译与启动，查看 Swagger UI 输出。
- 使用 `npm run build` / `npm run dev` 验证前端无 TypeScript/语法错误。

### 手动功能链路验证
- 使用测试账号 `admin / admin123` 登录系统。
- 模拟资产离线与 CPU 使用率破 90%，验证后台 BackgroundService 是否自动生成紧急告警并在前端告警中心展示。
- 执行告警处理操作，核实操作日志中是否精准记录了处置轨迹。
