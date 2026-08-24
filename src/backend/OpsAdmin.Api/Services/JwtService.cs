using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OpsAdmin.Api.Entities;

namespace OpsAdmin.Api.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(SysUser user, string roleCode, List<string> permissionCodes)
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? "OpsAdminPlatformVeryLongSuperSecretKey2026Net8Vue3!@#$";
            var issuer = _configuration["Jwt:Issuer"] ?? "OpsAdminServer";
            var audience = _configuration["Jwt:Audience"] ?? "OpsAdminClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("RealName", user.RealName ?? ""),
                new Claim(ClaimTypes.Role, roleCode)
            };

            foreach (var perm in permissionCodes)
            {
                claims.Add(new Claim("Permission", perm));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(4),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
