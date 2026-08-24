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
    public class LogController : ControllerBase
    {
        private readonly OpsDbContext _context;

        public LogController(OpsDbContext context)
        {
            _context = context;
        }

        [HttpGet("operation")]
        public async Task<ActionResult<ApiResponse<PagedResult<OperationLog>>>> GetOperationLogs(
            [FromQuery] string? username,
            [FromQuery] string? module,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var q = _context.OperationLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                q = q.Where(l => l.Username.Contains(username));
            }
            if (!string.IsNullOrWhiteSpace(module))
            {
                q = q.Where(l => l.Module == module);
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(l => l.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<OperationLog>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<OperationLog>>.Ok(result));
        }

        [HttpGet("system")]
        public async Task<ActionResult<ApiResponse<PagedResult<SystemLog>>>> GetSystemLogs(
            [FromQuery] string? level,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var q = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(level))
            {
                q = q.Where(l => l.Level == level);
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(l => l.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<SystemLog>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<SystemLog>>.Ok(result));
        }
    }
}
