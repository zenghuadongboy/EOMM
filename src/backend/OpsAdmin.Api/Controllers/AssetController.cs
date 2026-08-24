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
    public class AssetController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public AssetController(OpsDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<PagedResult<Asset>>>> GetList([FromQuery] AssetQueryRequest query)
        {
            var q = _context.Assets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                q = q.Where(a => a.Name.Contains(query.Keyword) || a.AssetNo.Contains(query.Keyword) || a.IpAddress.Contains(query.Keyword));
            }
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                q = q.Where(a => a.Type == query.Type);
            }
            if (!string.IsNullOrWhiteSpace(query.Department))
            {
                q = q.Where(a => a.Department == query.Department);
            }
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(a => a.Status == query.Status);
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(a => a.Id)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var result = new PagedResult<Asset>
            {
                Items = items,
                TotalCount = total,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };

            return Ok(ApiResponse<PagedResult<Asset>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Asset>>> GetById(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return Ok(ApiResponse<Asset>.Fail("资产未找到"));
            return Ok(ApiResponse<Asset>.Ok(asset));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<Asset>>> Save([FromBody] SaveAssetRequest req)
        {
            var username = User.Identity?.Name ?? "system";
            Asset? entity;

            if (req.Id == 0)
            {
                // Check duplicate AssetNo
                if (await _context.Assets.AnyAsync(a => a.AssetNo == req.AssetNo))
                {
                    return Ok(ApiResponse<Asset>.Fail($"设备编号 [{req.AssetNo}] 已存在"));
                }

                entity = new Asset
                {
                    AssetNo = req.AssetNo,
                    Name = req.Name,
                    Type = req.Type,
                    IpAddress = req.IpAddress,
                    Port = req.Port,
                    Department = req.Department,
                    Manager = req.Manager,
                    Status = req.Status,
                    HardwareSpec = req.HardwareSpec,
                    PurchaseDate = req.PurchaseDate,
                    MaintenanceExpire = req.MaintenanceExpire,
                    CpuUsage = 15.0,
                    MemoryUsage = 35.0,
                    DiskUsage = 20.0,
                    UpdatedAt = DateTime.Now
                };
                _context.Assets.Add(entity);
            }
            else
            {
                entity = await _context.Assets.FindAsync(req.Id);
                if (entity == null) return Ok(ApiResponse<Asset>.Fail("修改的资产不存在"));

                entity.Name = req.Name;
                entity.Type = req.Type;
                entity.IpAddress = req.IpAddress;
                entity.Port = req.Port;
                entity.Department = req.Department;
                entity.Manager = req.Manager;
                entity.Status = req.Status;
                entity.HardwareSpec = req.HardwareSpec;
                entity.PurchaseDate = req.PurchaseDate;
                entity.MaintenanceExpire = req.MaintenanceExpire;
                entity.UpdatedAt = DateTime.Now;
            }

            // Audit
            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
                Module = "资产管理",
                Action = req.Id == 0 ? "新增资产" : "修改资产",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ assetNo: '{req.AssetNo}', name: '{req.Name}' }}",
                Success = true,
                Description = $"保存资产 [{req.Name}] 信息成功",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<Asset>.Ok(entity, "保存设备资产信息成功"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            var username = User.Identity?.Name ?? "system";
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return Ok(ApiResponse<string>.Fail("被删除的资产不存在"));

            _context.Assets.Remove(asset);

            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
                Module = "资产管理",
                Action = "删除资产",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ id: {id} }}",
                Success = true,
                Description = $"删除了资产设备 [{asset.Name}]",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("删除资产成功"));
        }

        [HttpPost("simulate-metric")]
        public async Task<ActionResult<ApiResponse<Asset>>> SimulateMetric([FromBody] SimulateMetricRequest req)
        {
            var username = User.Identity?.Name ?? "system";
            var asset = await _context.Assets.FindAsync(req.AssetId);
            if (asset == null) return Ok(ApiResponse<Asset>.Fail("资产不存在"));

            if (!string.IsNullOrWhiteSpace(req.Status)) asset.Status = req.Status;
            if (req.CpuUsage.HasValue) asset.CpuUsage = req.CpuUsage.Value;
            if (req.MemoryUsage.HasValue) asset.MemoryUsage = req.MemoryUsage.Value;
            if (req.DiskUsage.HasValue) asset.DiskUsage = req.DiskUsage.Value;
            asset.UpdatedAt = DateTime.Now;

            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
                Module = "资产监控",
                Action = "指标模拟",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ assetId: {asset.Id}, cpu: {asset.CpuUsage}, status: '{asset.Status}' }}",
                Success = true,
                Description = $"手动模拟变更设备 [{asset.Name}] 指标参数",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<Asset>.Ok(asset, "模拟指标更新成功，告警引擎将在数秒内重新计算"));
        }
    }
}
