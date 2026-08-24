namespace OpsAdmin.Api.DTOs
{
    // Common ApiResponse
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string msg = "操作成功")
        {
            return new ApiResponse<T> { Success = true, Message = msg, Data = data };
        }

        public static ApiResponse<T> Fail(string msg)
        {
            return new ApiResponse<T> { Success = false, Message = msg, Data = default };
        }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    // Auth DTOs
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class ModifyPasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    // Asset DTOs
    public class AssetQueryRequest
    {
        public string? Keyword { get; set; }
        public string? Type { get; set; }
        public string? Department { get; set; }
        public string? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SaveAssetRequest
    {
        public int Id { get; set; }
        public string AssetNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Server";
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public string Department { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public string Status { get; set; } = "Online";
        public string HardwareSpec { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public DateTime MaintenanceExpire { get; set; } = DateTime.Now.AddYears(3);
    }

    public class SimulateMetricRequest
    {
        public int AssetId { get; set; }
        public string? Status { get; set; }
        public double? CpuUsage { get; set; }
        public double? MemoryUsage { get; set; }
        public double? DiskUsage { get; set; }
    }

    // Alarm DTOs
    public class ProcessAlarmRequest
    {
        public int AlarmId { get; set; }
        public string Status { get; set; } = "Resolved"; // Resolved or Ignored
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    // Dashboard DTOs
    public class DashboardOverviewDto
    {
        public int TotalAssets { get; set; }
        public int OnlineAssets { get; set; }
        public int FaultAssets { get; set; }
        public int PendingAlarms { get; set; }
        public double AverageCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }

        public List<ChartItem> AssetsByType { get; set; } = new List<ChartItem>();
        public List<ChartItem> AssetsByStatus { get; set; } = new List<ChartItem>();
        public List<ChartItem> AlarmsBySeverity { get; set; } = new List<ChartItem>();
        public List<TrendPoint> AlarmTrend24H { get; set; } = new List<TrendPoint>();
        public List<TopFaultAssetDto> TopFaultAssets { get; set; } = new List<TopFaultAssetDto>();
        public double AverageMttrHours { get; set; }
    }

    public class ChartItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class TrendPoint
    {
        public string TimeLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopFaultAssetDto
    {
        public string AssetName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int FaultCount { get; set; }
    }
}
