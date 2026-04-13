using System.ComponentModel.DataAnnotations;

namespace PingCRM.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Action { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }
}
