using UserManagmentSystem.Web.Models;
using UserManagmentSystem.Web.Models.Entities;
using UserManagmentSystem.Web.Services.Extensions;

namespace UserManagmentSystem.Web.Services.Interfaces;


public interface IUserService
{
    Task<ServiceResult> RegisterUserAsync(RegisterViewModel user);
    Task<ServiceResult> LogInUserAsync(LoginViewModel user);
    Task<ServiceResult> LogOutUserAsync();
    Task<ServiceResult> BlockUsersAsync(List<Guid> ids);
    Task<ServiceResult> DeleteUsersAsync(List<Guid> ids);
    Task<ServiceResult> DeleteUnverifiedUsers(List<Guid> ids);
    Task<ServiceResult> UnblockUsersAsync(List<Guid> ids);
    Task<List<User>> GetAllUsersAsync();
}
