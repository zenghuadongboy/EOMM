using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsAdmin.Api.Data;
using OpsAdmin.Api.DTOs;
using OpsAdmin.Api.Entities;
using OpsAdmin.Api.Services;

namespace OpsAdmin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly OpsDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(OpsDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Ok(ApiResponse<LoginResponse>.Fail("用户名或密码不能为空"));
            }

            var hash = DbInitializer.HashPassword(request.Password);
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == request.Username && u.PasswordHash == hash);

            if (user == null)
            {
                _context.OperationLogs.Add(new OperationLog
                {
                    Username = request.Username,
                    Module = "身份认证",
                    Action = "用户登录",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    RequestParam = $"{{ username: '{request.Username}' }}",
                    Success = false,
                    Description = "登录失败：用户名或密码错误",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<LoginResponse>.Fail("用户名或密码不正确"));
            }

            if (user.Status != "Enable")
            {
                return Ok(ApiResponse<LoginResponse>.Fail("账号已被禁用，请联系系统管理员"));
            }

            var userRole = user.UserRoles.FirstOrDefault()?.Role;
            var roleId = userRole?.Id ?? 0;
            var roleCode = userRole?.RoleCode ?? "Guest";
            var roleName = userRole?.RoleName ?? "未指派角色";

            List<string> permissions;
            if (roleCode == "Admin")
            {
                permissions = await _context.Permissions.Select(p => p.PermissionCode).ToListAsync();
            }
            else
            {
                var permissionIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                permissions = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .Select(p => p.PermissionCode)
                    .ToListAsync();
            }

            var token = _jwtService.GenerateToken(user, roleCode, permissions);

            _context.OperationLogs.Add(new OperationLog
            {
                Username = user.UserName,
                Module = "身份认证",
                Action = "用户登录",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = $"{{ username: '{user.UserName}' }}",
                Success = true,
                Description = $"用户 [{user.RealName}] 成功登录",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                Username = user.UserName,
                RealName = user.RealName,
                RoleCode = roleCode,
                RoleName = roleName,
                Permissions = permissions
            };

            return Ok(ApiResponse<LoginResponse>.Ok(response, "登录成功"));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null) return Ok(ApiResponse<object>.Fail("用户不存在"));

            var userRole = user.UserRoles.FirstOrDefault()?.Role;
            var roleId = userRole?.Id ?? 0;

            List<string> permissions;
            if (userRole?.RoleCode == "Admin")
            {
                permissions = await _context.Permissions.Select(p => p.PermissionCode).ToListAsync();
            }
            else
            {
                var permissionIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                permissions = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .Select(p => p.PermissionCode)
                    .ToListAsync();
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id,
                Username = user.UserName,
                user.RealName,
                user.Email,
                user.Status,
                RoleCode = userRole?.RoleCode,
                RoleName = userRole?.RoleName,
                Permissions = permissions
            }));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ModifyPasswordRequest request)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return Ok(ApiResponse<string>.Fail("未找到用户"));

            var oldHash = DbInitializer.HashPassword(request.OldPassword);
            if (user.PasswordHash != oldHash)
            {
                return Ok(ApiResponse<string>.Fail("原密码错误"));
            }

            user.PasswordHash = DbInitializer.HashPassword(request.NewPassword);

            _context.OperationLogs.Add(new OperationLog
            {
                Username = user.UserName,
                Module = "身份认证",
                Action = "修改密码",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                RequestParam = "******",
                Success = true,
                Description = "用户修改了登录密码",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Ok("密码修改成功，请重新登录"));
        }
    }
}
