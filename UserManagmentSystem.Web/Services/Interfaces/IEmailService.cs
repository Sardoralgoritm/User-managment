using UserManagmentSystem.Web.Services.Extensions;

namespace UserManagmentSystem.Web.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string name, string token, Guid userId);
    Task<bool> VerifyEmail(Guid userId, string token);
    Task<bool> SendPasswordResetAsync(string email, string name, string resetToken);
    Task<ServiceResult> ValidatePasswordResetTokenAsync(Guid userId, string token);
    Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword);
}
