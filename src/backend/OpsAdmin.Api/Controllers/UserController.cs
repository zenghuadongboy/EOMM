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
                .Include(u => u.Role)
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.RealName,
                    u.Email,
                    u.Phone,
                    u.Department,
                    u.RoleId,
                    RoleName = u.Role != null ? u.Role.Name : "无",
                    RoleCode = u.Role != null ? u.Role.Code : "",
                    u.IsEnabled,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<object>>.Ok(users.Cast<object>().ToList()));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveUser([FromBody] SysUser req)
        {
            var username = User.Identity?.Name ?? "system";

            if (req.Id == 0)
            {
                if (await _context.Users.AnyAsync(u => u.Username == req.Username))
                {
                    return Ok(ApiResponse<string>.Fail($"用户名 [{req.Username}] 已存在"));
                }

                req.PasswordHash = DbInitializer.HashPassword("123456"); // Default initial password
                req.CreatedAt = DateTime.Now;
                _context.Users.Add(req);
            }
            else
            {
                var user = await _context.Users.FindAsync(req.Id);
                if (user == null) return Ok(ApiResponse<string>.Fail("用户不存在"));

                user.RealName = req.RealName;
                user.Email = req.Email;
                user.Phone = req.Phone;
                user.Department = req.Department;
                user.RoleId = req.RoleId;
                user.IsEnabled = req.IsEnabled;
            }

            _context.OperationLogs.Add(new OperationLog
            {
                Username = username,
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

            user.IsEnabled = !user.IsEnabled;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok($"已{(user.IsEnabled ? "启用" : "禁用")}用户账号"));
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
}
