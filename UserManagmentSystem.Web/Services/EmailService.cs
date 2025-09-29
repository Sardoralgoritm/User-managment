using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using UserManagmentSystem.Web.Data;
using UserManagmentSystem.Web.Models.Entities;
using UserManagmentSystem.Web.Services.Extensions;
using UserManagmentSystem.Web.Services.Interfaces;

namespace UserManagmentSystem.Web.Services;

public class EmailService : IEmailService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _password;
    private readonly ILogger<EmailService> _logger;
    private IConfiguration _configuration;

    public EmailService(IHttpContextAccessor httpContextAccessor,
                        AppDbContext dbContext,
                        IPasswordService password,
                        ILogger<EmailService> logger,
                        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _password = password;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailVerificationAsync(string email, string name, string token, Guid userId)
    {
        if (_httpContextAccessor.HttpContext == null) return;
        var request = _httpContextAccessor.HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var verificationLink = $"{baseUrl}/Account/VerifyEmail?userId={userId}&token={token}";
        var subject = "Verify Your Email - THE APP";

        var body = $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background-color: #f4f4f7;
                        color: #333;
                        margin: 0;
                        padding: 0;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 30px auto;
                        background: #ffffff;
                        border-radius: 8px;
                        box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                        overflow: hidden;
                    }}
                    .header {{
                        background: linear-gradient(90deg, #667eea, #764ba2);
                        color: white;
                        padding: 20px;
                        text-align: center;
                    }}
                    .header h1 {{
                        margin: 0;
                        font-size: 24px;
                    }}
                    .content {{
                        padding: 20px;
                        line-height: 1.6;
                    }}
                    .button {{
                        display: inline-block;
                        background: #667eea;
                        color: white !important;
                        padding: 12px 24px;
                        border-radius: 5px;
                        text-decoration: none;
                        font-weight: bold;
                        margin: 20px 0;
                    }}
                    .footer {{
                        background-color: #f4f4f7;
                        text-align: center;
                        padding: 15px;
                        font-size: 12px;
                        color: #777;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Verify Your Email</h1>
                    </div>
                    <div class='content'>
                        <p>Hello <strong>{name}</strong>,</p>
                        <p>Thanks for signing up for <strong>THE APP</strong>. To complete your registration, please verify your email address by clicking the button below:</p>
                        <p style='text-align: center;'>
                            <a href='{verificationLink}' class='button'>Verify Email</a>
                        </p>
                        <p><small>This link will expire in 24 hours for your security.</small></p>
                        <p>If you didn’t create an account, you can safely ignore this email.</p>
                        <p>Cheers,<br>The APP Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.UtcNow.Year} THE APP. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task<bool> VerifyEmail(Guid userId, string token)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.EmailVerificationToken != token || user.EmailVerificationExpiry < DateTime.UtcNow)
        {
            return false;
        }

        user.Status = Models.Entities.Status.Active;
        user.EmailVerificationExpiry = null;
        user.EmailVerificationToken = string.Empty;

        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendPasswordResetAsync(string email, string name, string resetToken)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.Status == Status.Blocked)
                return false;

            user.PasswordResetToken = resetToken;
            user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            if (_httpContextAccessor.HttpContext == null) return false;

            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var resetLink = $"{baseUrl}/Account/ResetPassword?userId={user.Id}&token={resetToken}";

            var subject = "Reset Your Password - THE APP";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .button {{ 
                            background-color: #e53e3e; 
                            color: white; 
                            padding: 12px 24px; 
                            text-decoration: none; 
                            border-radius: 5px; 
                            display: inline-block;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Hello {name},</h2>
                        <p>We received a request to reset your password.</p>
                        <p>Click the button below to set a new password:</p>
                        <a href='{resetLink}' class='button'>Reset Password</a>
                        <p><small>This link will expire in 1 hour.</small></p>
                        <p>If you didn't request this, you can safely ignore this email.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(user.Email, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", email);
            return false;
        }
    }

    public async Task<ServiceResult> ValidatePasswordResetTokenAsync(Guid userId, string token)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.PasswordResetToken == token);

        if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
            return new ServiceResult { Success = false, Message = "Token expired" };

        return new ServiceResult { Success = true, Message = "Successful" };
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.PasswordResetToken == token);

        if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
            return false;

        string salt = BCrypt.Net.BCrypt.GenerateSalt();
        string hash = _password.HashPassword(newPassword, salt);

        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        user.PasswordResetToken = "";
        user.PasswordResetExpiry = null;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var email = _configuration.GetValue<string>("EMAIL_CONFIGURATION:EMAIL");
            var password = _configuration.GetValue<string>("EMAIL_CONFIGURATION:PASSWORD");
            var host = _configuration.GetValue<string>("EMAIL_CONFIGURATION:HOST");
            var port = _configuration.GetValue<int>("EMAIL_CONFIGURATION:PORT");

            var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(email, password)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email!, "Intern member"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return false;
        }
    }
}