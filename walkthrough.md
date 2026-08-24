# 基于 .NET 的企业运维管理平台设计与实现 - 交付成果与系统完成说明

本项目严格依据《毕业论文开题报告.txt》中定义的需求分析与架构规范，完成了从后端架构、数据库设计、安全审计，到前端富 UI 可视化控制台的全栈开发落地。

---

## 1. 完成的核心架构与功能模块

### ① 用户与身份权限管理模块 (RBAC + JWT)
- **无状态身份认证**：后端基于 ASP.NET Core `JwtBearer` 颁发 Token（包含 UserClaims、RoleCode 及 Permissions 权限清单）。
- **细粒度 RBAC 控制**：支持菜单级、按钮级与接口级动态授权。
- **内置预置账号**：
  - `admin / admin123`：超级管理员（具备全量修改、用户分配、规则配置及大屏权限）
  - `ops / ops123`：运维工程师（具备资产维保、告警响应处置、压力测试权限）
  - `viewer / viewer123`：只读访客（仅具备只读查看看板与日志权限）

### ② 设备资产全生命周期管理模块
- **设备台账**：覆盖物理服务器、云主机、网络设备（交换机/防火墙）、存储设备、数据库等全量资产维度。
- **数据指标与监视**：CPU 使用率、内存占用、磁盘空间、设备端口与 IP 绑扎，提供直观的动态 Progress 指示条。
- **故障与压力模拟器**：支持运维人员在线给任意设备投掷“资源突增/离线中断”测试参数，即时触发告警流程。

### ③ 自动化告警引擎与工单闭环处置模块
- **定时/实时轮询引擎**：基于 `BackgroundService` 挂载 `AlarmMonitoringBackgroundService`，后台每隔 20s 自动评估 12+ 台资产的运行指标与 `AlarmRules` 规则。
- **告警流转闭环**：`Pending (待处理)` -> `Processing (处理中)` -> `Resolved (已解决)` / `Ignored (已忽略)`，记录处理时间戳、响应人及故障复盘笔记。
- **实时通知**：前端顶部栏具备 Badge 警报红点与抽屉式未处理告警拉取功能。

### ④ 全量日志与操作审计模块
- **操作行为审计日志 (`OperationLog`)**：记录全量用户的登录、数据修改、资产删除、告警处置、权限修改等动作，精准记录 IP、模块、耗时及请求参数，不可删除篡改。
- **后台服务运行日志 (`SystemLog`)**：记录定时引擎的自动化评估情况与系统报错事件。

### ⑤ 运维数据可视化大屏模块 (`Dashboard`)
- **4 大概览卡片**：设备总量、正常运行率、故障/脱机资产数、待处置警报数。
- **ECharts 5 图表渲染**：
  - 24 小时告警触发趋势折线图/面积渐变图
  - 资产架构分类占比饼图
  - 平均故障修复时间 (MTTR) 指标分析
  - 故障发生频次最高的设备 Top 5 List

---

## 2. 工程目录与源代码位置

- **后端工程 (C# ASP.NET Core 8.0 API)**:
  - 接口入口与 DI 配置：[Program.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Program.cs)
  - EF Core 数据库与种子数据：[OpsDbContext.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Data/OpsDbContext.cs) / [DbInitializer.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Data/DbInitializer.cs)
  - 自动告警检测引擎：[AlarmMonitoringBackgroundService.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Services/AlarmMonitoringBackgroundService.cs)
  - 控制器集合：[AuthController.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Controllers/AuthController.cs), [AssetController.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Controllers/AssetController.cs), [AlarmController.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Controllers/AlarmController.cs), [DashboardController.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Controllers/DashboardController.cs), [LogController.cs](file:///d:/大学/基于.NET的企业运维管理平/src/backend/OpsAdmin.Api/Controllers/LogController.cs)

- **前端工程 (Vue 3 + Vite + Element Plus + ECharts)**:
  - 启动与路由：[main.js](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/main.js) / [index.js](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/router/index.js)
  - 全局暗黑 Glassmorphism 主题：[main.css](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/assets/main.css)
  - 登录视图：[Index.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/login/Index.vue)
  - 主界面与通知抽屉 Layout：[MainLayout.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/layout/MainLayout.vue)
  - 运维可视化大屏：[Index.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/dashboard/Index.vue)
  - 设备资产全生命周期控制台：[List.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/assets/List.vue)
  - 告警处置与策略配置：[List.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/alarms/List.vue) / [Rules.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/alarms/Rules.vue)
  - 审计日志：[Logs.vue](file:///d:/大学/基于.NET的企业运维管理平/src/frontend/src/views/system/Logs.vue)

---

## 3. 系统运行与验证方式

### 后端 API 启动
在工程根目录运行：
```powershell
dotnet run --project src/backend/OpsAdmin.Api/OpsAdmin.Api.csproj
```
服务将在 `http://localhost:5000` 启动，并会自动在后台完成 `opsadmin.db` SQLite 数据库建表与种子初始化。可在浏览器中打开 `http://localhost:5000/swagger` 查阅 Swagger OpenAPI 文档。

### 前端应用启动
在 `src/frontend` 目录运行：
```powershell
npm run dev
```
访问 `http://localhost:3000` 即可进入高保真企业运维管理平台！
