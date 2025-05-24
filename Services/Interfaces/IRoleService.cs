using BlazorApp1.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Services.Interfaces
{
    public interface IRoleService
    {
        Task<RoleDto?> GetByNameAsync(string roleName);
        Task<IEnumerable<RoleDto>> GetAllAsync();
    }
}