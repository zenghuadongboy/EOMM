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
    public class PermissionController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public PermissionController(OpsDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<PagedResult<SysPermission>>>> GetList(
            [FromQuery] string? keyword,
            [FromQuery] string? method,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var q = _context.Permissions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(p => p.PermissionCode.Contains(keyword) 
                              || p.PermissionName.Contains(keyword) 
                              || p.Path.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(method))
            {
                q = q.Where(p => p.Method == method);
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(p => p.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<SysPermission>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<SysPermission>>.Ok(result));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<SysPermission>>> Save([FromBody] PermissionSaveDto req)
        {
            var currentUsername = User.Identity?.Name ?? "system";
            SysPermission? perm;

            if (req.Id == 0)
            {
                if (await _context.Permissions.AnyAsync(p => p.PermissionCode == req.PermissionCode))
                {
                    return Ok(ApiResponse<SysPermission>.Fail($"权限编码 [{req.PermissionCode}] 已存在"));
                }

                perm = new SysPermission
                {
                    PermissionCode = req.PermissionCode,
                    PermissionName = req.PermissionName,
                    Path = req.Path,
                    Method = string.IsNullOrWhiteSpace(req.Method) ? "GET" : req.Method.ToUpper(),
                    Description = req.Description,
                    CreateTime = DateTime.Now
                };
                _context.Permissions.Add(perm);
            }
            else
            {
                perm = await _context.Permissions.FindAsync(req.Id);
                if (perm == null) return Ok(ApiResponse<SysPermission>.Fail("权限节点不存在"));

                perm.PermissionName = req.PermissionName;
                perm.Path = req.Path;
                perm.Method = string.IsNullOrWhiteSpace(req.Method) ? "GET" : req.Method.ToUpper();
                perm.Description = req.Description;
            }

            _context.OperationLogs.Add(new OperationLog
            {
                Username = currentUsername,
                Module = "权限管理",
                Action = req.Id == 0 ? "新增权限点" : "修改权限点",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ code: '{req.PermissionCode}', name: '{req.PermissionName}' }}",
                Success = true,
                Description = $"保存权限点 [{req.PermissionName}] 信息成功",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<SysPermission>.Ok(perm, "权限节点信息保存成功"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            var currentUsername = User.Identity?.Name ?? "system";
            var perm = await _context.Permissions.FindAsync(id);
            if (perm == null) return Ok(ApiResponse<string>.Fail("被删除的权限点不存在"));

            // 清理角色与权限映射表里的关联
            var rolePerms = _context.RolePermissions.Where(rp => rp.PermissionId == id);
            _context.RolePermissions.RemoveRange(rolePerms);

            _context.Permissions.Remove(perm);

            _context.OperationLogs.Add(new OperationLog
            {
                Username = currentUsername,
                Module = "权限管理",
                Action = "删除权限点",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ id: {id} }}",
                Success = true,
                Description = $"删除了权限节点 [{perm.PermissionName}] ({perm.PermissionCode})",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok($"权限节点 [{perm.PermissionName}] 删除成功"));
        }
    }

    public class PermissionSaveDto
    {
        public int Id { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string Description { get; set; } = string.Empty;
    }
}
