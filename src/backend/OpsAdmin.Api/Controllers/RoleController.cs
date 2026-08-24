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
    public class RoleController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public RoleController(OpsDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            var result = new List<object>();

            foreach (var r in roles)
            {
                var permIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == r.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                result.Add(new
                {
                    r.Id,
                    r.Code,
                    r.Name,
                    r.Description,
                    r.CreatedAt,
                    PermissionIds = permIds
                });
            }

            return Ok(ApiResponse<List<object>>.Ok(result));
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<ApiResponse<List<SysPermission>>>> GetPermissions()
        {
            var perms = await _context.Permissions.OrderBy(p => p.SortOrder).ToListAsync();
            return Ok(ApiResponse<List<SysPermission>>.Ok(perms));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveRole([FromBody] SysRole role)
        {
            if (role.Id == 0)
            {
                if (await _context.Roles.AnyAsync(r => r.Code == role.Code))
                {
                    return Ok(ApiResponse<string>.Fail($"角色代码 [{role.Code}] 已存在"));
                }
                role.CreatedAt = DateTime.Now;
                _context.Roles.Add(role);
            }
            else
            {
                var existing = await _context.Roles.FindAsync(role.Id);
                if (existing == null) return Ok(ApiResponse<string>.Fail("角色不存在"));

                existing.Name = role.Name;
                existing.Description = role.Description;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("角色信息保存成功"));
        }

        public class AssignPermReq
        {
            public int RoleId { get; set; }
            public List<int> PermissionIds { get; set; } = new List<int>();
        }

        [HttpPost("assign-permissions")]
        public async Task<ActionResult<ApiResponse<string>>> AssignPermissions([FromBody] AssignPermReq req)
        {
            var role = await _context.Roles.FindAsync(req.RoleId);
            if (role == null) return Ok(ApiResponse<string>.Fail("角色不存在"));

            var existingPerms = _context.RolePermissions.Where(rp => rp.RoleId == req.RoleId);
            _context.RolePermissions.RemoveRange(existingPerms);

            foreach (var permId in req.PermissionIds)
            {
                _context.RolePermissions.Add(new SysRolePermission { RoleId = req.RoleId, PermissionId = permId });
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("权限授权更新成功"));
        }
    }
}
