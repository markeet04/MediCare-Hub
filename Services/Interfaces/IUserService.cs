using BlazorApp1.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Services.Interfaces
{
    public interface IUserService
    {
        Task<int> CreateUserAsync(UserDto userDto, string password, string roleName);
        Task<UserDto?> GetByCnicAsync(string cnic);
        Task DeleteUserAsync(int userId);
        Task UpdateUserAsync(UserDto userDto);
        Task<IEnumerable<UserDto>> GetAllByRoleAsync(string roleName);
        Task<string> GetUserRoleAsync(int userId);
    }
}