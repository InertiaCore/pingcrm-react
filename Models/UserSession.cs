using System.ComponentModel.DataAnnotations;

namespace PingCRM.Models;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string SessionToken { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
