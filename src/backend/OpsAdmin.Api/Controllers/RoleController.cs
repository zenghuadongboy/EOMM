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
                    Code = r.RoleCode,
                    Name = r.RoleName,
                    RoleCode = r.RoleCode,
                    RoleName = r.RoleName,
                    r.Description,
                    CreatedAt = r.CreateTime,
                    PermissionIds = permIds
                });
            }

            return Ok(ApiResponse<List<object>>.Ok(result));
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<ApiResponse<List<SysPermission>>>> GetPermissions()
        {
            var perms = await _context.Permissions.OrderBy(p => p.Id).ToListAsync();
            return Ok(ApiResponse<List<SysPermission>>.Ok(perms));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveRole([FromBody] RoleSaveDto req)
        {
            SysRole? role;
            if (req.Id == 0)
            {
                if (await _context.Roles.AnyAsync(r => r.RoleCode == req.RoleCode))
                {
                    return Ok(ApiResponse<string>.Fail($"角色代码 [{req.RoleCode}] 已存在"));
                }
                role = new SysRole
                {
                    RoleCode = req.RoleCode,
                    RoleName = req.RoleName,
                    Description = req.Description,
                    CreateTime = DateTime.Now
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }
            else
            {
                role = await _context.Roles.FindAsync(req.Id);
                if (role == null) return Ok(ApiResponse<string>.Fail("角色不存在"));

                role.RoleName = req.RoleName;
                role.Description = req.Description;
                await _context.SaveChangesAsync();
            }

            // 同步更新分配的权限点
            if (req.PermissionIds != null)
            {
                var existingPerms = _context.RolePermissions.Where(rp => rp.RoleId == role.Id);
                _context.RolePermissions.RemoveRange(existingPerms);

                foreach (var permId in req.PermissionIds)
                {
                    _context.RolePermissions.Add(new SysRolePermission { RoleId = role.Id, PermissionId = permId });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(ApiResponse<string>.Ok("角色及权限保存成功"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return Ok(ApiResponse<string>.Fail("角色不存在"));

            var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
            if (hasUsers)
            {
                return Ok(ApiResponse<string>.Fail($"无法删除角色 [{role.RoleName}]，当前已有用户关联此角色"));
            }

            var perms = _context.RolePermissions.Where(rp => rp.RoleId == id);
            _context.RolePermissions.RemoveRange(perms);
            _context.Roles.Remove(role);

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok($"角色 [{role.RoleName}] 删除成功"));
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

    public class RoleSaveDto
    {
        public int Id { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<int>? PermissionIds { get; set; }
    }
}
