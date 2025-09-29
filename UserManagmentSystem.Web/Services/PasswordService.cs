using UserManagmentSystem.Web.Services.Interfaces;

namespace UserManagmentSystem.Web.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password, string salt)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, salt);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}