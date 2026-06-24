namespace IdentityService.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; } = null!;
    public string? Username { get; set; }
    public string PasswordHash { get; set; } = null!;
    public int Role { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int TokenVersion { get; set; } = 1;
    public string? Bio { get; set; }
    public DateTime? Birthday { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool NotifyNewInCollection { get; set; } = true;
    public bool NotifyVideoAdded { get; set; } = true;
    public bool NotifyFileAdded { get; set; } = true;
}
