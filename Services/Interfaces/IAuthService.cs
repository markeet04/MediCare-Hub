using BlazorApp1.Models.DTOs;

namespace BlazorApp1.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> ValidateCredentialsAsync(string userName, string password);
        Task<string?> GetUserRoleAsync(int userId);
        Task<bool> IsUserInRoleAsync(int userId, string role);
    }
} 