namespace UserManagmentSystem.Web.Services.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password, string salt);
    bool VerifyPassword(string password, string hash);
}