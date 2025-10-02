using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagmentSystem.Web.Data;
using UserManagmentSystem.Web.Models;
using UserManagmentSystem.Web.Models.Entities;
using UserManagmentSystem.Web.Services.Extensions;
using UserManagmentSystem.Web.Services.Interfaces;

namespace UserManagmentSystem.Web.Services;

public class UserService : IUserService
{
    private readonly AppDbContext dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db,
           IPasswordService ps,
           IEmailService emailService,
           IHttpContextAccessor httpContext,
           ILogger<UserService> logger)
    {
        dbContext = db;
        _passwordService = ps;
        _emailService = emailService;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task<ServiceResult> BlockUsersAsync(List<Guid> ids)
    {
        if (ids == null)
        {
            return new ServiceResult { Message = "Users are not selected!", Success = false };
        }

        var users = await dbContext.Users.Where(u => ids.Contains(u.Id) && u.Status != Status.Blocked).ToListAsync();
        foreach (var user in users)
        {
            user.Status = Status.Blocked;
            dbContext.Users.Update(user);
        }

        await dbContext.SaveChangesAsync();

        return new ServiceResult { Message = "Users blocked successfully!", Success = true };
    }

    public async Task<ServiceResult> DeleteUnverifiedUsers(List<Guid> ids)
    {
        if (ids == null)
        {
            return new ServiceResult { Message = "Any user selected", Success = false };
        }

        var users = await dbContext.Users.Where(u => ids.Contains(u.Id) && u.Status == Status.Unverified).ToListAsync();

        dbContext.Users.RemoveRange(users);
        await dbContext.SaveChangesAsync();

        return new ServiceResult { Message = "Unverified users deleted!", Success = true };
    }

    public async Task<ServiceResult> DeleteUsersAsync(List<Guid> ids)
    {
        if (ids == null)
        {
            return new ServiceResult { Message = "Any user selected!", Success = false };
        }

        var users = await dbContext.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

        dbContext.Users.RemoveRange(users);

        await dbContext.SaveChangesAsync();
        return new ServiceResult { Message = "Users deleted successfully!", Success = false };
    }

    public async Task<List<User>> GetAllUsersAsync()
        => await dbContext.Users.OrderByDescending(u => u.LastLogInTime).ToListAsync();

    public async Task<ServiceResult> LogInUserAsync(LoginViewModel user)
    {
        try
        {
            var existUser = await dbContext.Users.FirstOrDefaultAsync(us => us.Email == user.Email);

            if (existUser == null)
            {
                _logger.LogWarning("The email is not found!");
                return new ServiceResult { Message = "The email is not found!", Success = false};
            }

            if (!_passwordService.VerifyPassword(user.Password, existUser.PasswordHash))
            {
                _logger.LogWarning("Password is incorrect!");
                return new ServiceResult { Message = "Password is incorrect!", Success = false };
            }

            if (existUser.Status == Status.Blocked)
            {
                _logger.LogWarning("The user is blocked!");
                return new ServiceResult { Message = "The user is blocked!", Success = false };
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, existUser.Id.ToString()),
                new Claim(ClaimTypes.Name, existUser.Name),
                new Claim(ClaimTypes.Email, existUser.Email),
                new Claim("Position", existUser.Position)
            };

            var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = user.RememberMe,
                ExpiresUtc = user.RememberMe ? DateTimeOffset.UtcNow.AddDays(15) : DateTimeOffset.UtcNow.AddHours(1)
            };

            if (_httpContext.HttpContext == null)
            {
                _logger.LogWarning("Something went wrong!");
                return new ServiceResult { Message = "Something went wrong!", Success = false };
            }

            await _httpContext.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

            existUser.LastLogInTime = DateTime.UtcNow;
            dbContext.Users.Update(existUser);
            await dbContext.SaveChangesAsync();

            return new ServiceResult { Message = "Log in successfully!", Success = true }; ;
        }
        catch
        {
            return new ServiceResult { Message = "Something went wrong!", Success = false };
        }
    }
                      
    public async Task<ServiceResult> LogOutUserAsync()
    {
        try
        {
            if (_httpContext.HttpContext == null)
            {
                _logger.LogWarning("Something went wrong!");
                return new ServiceResult { Message = "Something went wrong!", Success = false };
            }

            await _httpContext.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return new ServiceResult { Success = true, Message = "Log out successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing out user");
            return new ServiceResult { Message = "Error signing out user", Success = false };
        }
    }
                      
    public async Task<ServiceResult> RegisterUserAsync(RegisterViewModel user)
    {
        try
        {
            if (await dbContext.Users.AnyAsync(i => i.Email == user.Email))
            {
                _logger.LogWarning("Registration failed: Email {0} already exists", user.Email);
                return new ServiceResult { Message = $"Registration failed: Email {user.Email} already exists", Success = false };
            }

            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string hash = _passwordService.HashPassword(user.Password, salt);
            var token = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddHours(24);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Name = user.Name,
                Email = user.Email,
                Position = user.Position,
                PasswordHash = hash,
                PasswordSalt = salt,
                Status = Status.Unverified,
                CreateAt = DateTime.UtcNow,
                EmailVerificationToken = token,
                EmailVerificationExpiry = expiry,
                LastLogInTime = DateTime.MinValue
            };

            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();

            await _emailService.SendEmailVerificationAsync(user.Email, user.Name, token, newUser.Id);

            _logger.LogInformation($"User {user.Email} registered successfully with Unverified status");
            return new ServiceResult { Message = $"User {user.Email} registered successfully with Unverified status", Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering user {user.Email}");
            return new ServiceResult { Success = false, Message = $"Error registering user {user.Email}" };
        }
    }

    public async Task<ServiceResult> UnblockUsersAsync(List<Guid> ids)
    {
        if (ids == null)
        {
            return new ServiceResult { Message = "Users are not selected!", Success = false };
        }

        var users = await dbContext.Users.Where(u => ids.Contains(u.Id) && u.Status == Status.Blocked).ToListAsync();
        foreach (var user in users)
        {
            user.Status = Status.Active;
            dbContext.Users.Update(user);
        }

        await dbContext.SaveChangesAsync();

        return new ServiceResult { Message = "Users unblocked successfully!", Success = true };
    }
}
