using System.Globalization;

namespace UserManagmentSystem.Web.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string EmailVerificationToken { get; set; } = string.Empty;
    public DateTime? EmailVerificationExpiry { get; set; }
    public string PasswordResetToken { get; set; } = string.Empty;
    public DateTime? PasswordResetExpiry { get; set; }
    public Status Status { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime LastLogInTime { get; set; }
}
