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
    public class UserController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public UserController(OpsDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            var list = users.Select(u => {
                var role = u.UserRoles.FirstOrDefault()?.Role;
                return new
                {
                    u.Id,
                    Username = u.UserName,
                    u.RealName,
                    u.Email,
                    RoleId = role?.Id ?? 0,
                    RoleName = role != null ? role.RoleName : "无",
                    RoleCode = role != null ? role.RoleCode : "",
                    Status = u.Status,
                    IsEnabled = u.Status == "Enable",
                    CreatedAt = u.CreateTime
                };
            }).ToList();

            return Ok(ApiResponse<List<object>>.Ok(list.Cast<object>().ToList()));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveUser([FromBody] UserSaveDto req)
        {
            var currentUsername = User.Identity?.Name ?? "system";

            SysUser? user;
            if (req.Id == 0)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == req.Username))
                {
                    return Ok(ApiResponse<string>.Fail($"用户名 [{req.Username}] 已存在"));
                }

                user = new SysUser
                {
                    UserName = req.Username,
                    RealName = req.RealName,
                    Email = req.Email,
                    Status = req.IsEnabled ? "Enable" : "Disable",
                    PasswordHash = DbInitializer.HashPassword("123456"),
                    CreateTime = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (req.RoleId > 0)
                {
                    _context.UserRoles.Add(new SysUserRole
                    {
                        UserId = user.Id,
                        RoleId = req.RoleId,
                        CreateTime = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == req.Id);

                if (user == null) return Ok(ApiResponse<string>.Fail("用户不存在"));

                user.RealName = req.RealName;
                user.Email = req.Email;
                user.Status = req.IsEnabled ? "Enable" : "Disable";

                // 更新用户角色
                _context.UserRoles.RemoveRange(user.UserRoles);
                if (req.RoleId > 0)
                {
                    _context.UserRoles.Add(new SysUserRole
                    {
                        UserId = user.Id,
                        RoleId = req.RoleId,
                        CreateTime = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
            }

            _context.OperationLogs.Add(new OperationLog
            {
                Username = currentUsername,
                Module = "用户管理",
                Action = req.Id == 0 ? "新增用户" : "修改用户",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ username: '{req.Username}' }}",
                Success = true,
                Description = $"保存用户 [{req.Username}] 信息成功",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("用户信息保存成功"));
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Ok(ApiResponse<string>.Fail("用户不存在"));

            user.Status = user.Status == "Enable" ? "Disable" : "Enable";
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok($"已{(user.Status == "Enable" ? "启用" : "禁用")}用户账号"));
        }

        [HttpPost("reset-password/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Ok(ApiResponse<string>.Fail("用户不存在"));

            user.PasswordHash = DbInitializer.HashPassword("123456");
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("密码已成功重置为默认密码 123456"));
        }
    }

    public class UserSaveDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
