using PingCRM.Data;
using PingCRM.Models;

namespace PingCRM.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(HttpContext httpContext, int? userId, string action, string? details = null)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Action = action,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString().Length > 512
                ? httpContext.Request.Headers.UserAgent.ToString()[..512]
                : httpContext.Request.Headers.UserAgent.ToString(),
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();
    }
}
