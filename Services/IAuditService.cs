namespace PingCRM.Services;

public interface IAuditService
{
    Task LogAsync(HttpContext context, int? userId, string action, string? details = null);
}
