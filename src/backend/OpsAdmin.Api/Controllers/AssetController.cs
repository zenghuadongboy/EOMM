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

        [HttpGet("types")]
        public async Task<ActionResult<ApiResponse<List<AssetType>>>> GetAssetTypes()
        {
            var types = await _context.AssetTypes.OrderBy(t => t.Id).ToListAsync();
            return Ok(ApiResponse<List<AssetType>>.Ok(types));
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<PagedResult<Asset>>>> GetList([FromQuery] AssetQueryRequest query)
        {
            var q = _context.Assets
                .Include(a => a.AssetType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                q = q.Where(a => a.AssetName.Contains(query.Keyword) || a.AssetNo.Contains(query.Keyword) || a.IpAddress.Contains(query.Keyword));
            }
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                q = q.Where(a => a.AssetType != null && a.AssetType.TypeName == query.Type);
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
            var asset = await _context.Assets
                .Include(a => a.AssetType)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null) return Ok(ApiResponse<Asset>.Fail("资产未找到"));
            return Ok(ApiResponse<Asset>.Ok(asset));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<Asset>>> Save([FromBody] SaveAssetRequest req)
        {
            var username = User.Identity?.Name ?? "system";
            Asset? entity;

            // 获取/校验设备类型
            var assetType = await _context.AssetTypes.FirstOrDefaultAsync(t => t.TypeName == req.Type || t.TypeCode == req.Type)
                            ?? await _context.AssetTypes.FirstOrDefaultAsync();

            var assetTypeId = assetType?.Id ?? 1;

            if (req.Id == 0)
            {
                if (await _context.Assets.AnyAsync(a => a.AssetNo == req.AssetNo))
                {
                    return Ok(ApiResponse<Asset>.Fail($"设备编号 [{req.AssetNo}] 已存在"));
                }

                entity = new Asset
                {
                    AssetNo = req.AssetNo,
                    AssetName = req.Name,
                    AssetTypeId = assetTypeId,
                    IpAddress = req.IpAddress,
                    Location = req.Department, // 使用部门或具体机房作为位置
                    Status = req.Status,
                    Description = string.IsNullOrWhiteSpace(req.HardwareSpec) ? $"责任人: {req.Manager}" : req.HardwareSpec,
                    CreateTime = DateTime.Now
                };
                _context.Assets.Add(entity);
            }
            else
            {
                entity = await _context.Assets.FindAsync(req.Id);
                if (entity == null) return Ok(ApiResponse<Asset>.Fail("修改的资产不存在"));

                entity.AssetName = req.Name;
                entity.AssetTypeId = assetTypeId;
                entity.IpAddress = req.IpAddress;
                entity.Location = req.Department;
                entity.Status = req.Status;
                entity.Description = string.IsNullOrWhiteSpace(req.HardwareSpec) ? $"责任人: {req.Manager}" : req.HardwareSpec;
            }

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
                Description = $"删除了资产设备 [{asset.AssetName}]",
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

            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
                Module = "资产监控",
                Action = "指标模拟",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ assetId: {asset.Id}, status: '{asset.Status}' }}",
                Success = true,
                Description = $"手动模拟变更设备 [{asset.AssetName}] 状态",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<Asset>.Ok(asset, "模拟状态更新成功"));
        }
    }
}
