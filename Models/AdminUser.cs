using System.ComponentModel.DataAnnotations;

namespace GSCFacultyTimeMonitoring.Models;

public class AdminUser
{
    public int AdminUserId { get; set; }

    [Required, StringLength(80)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = "GSC Administrator";

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
