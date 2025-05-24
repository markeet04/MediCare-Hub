using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace BlazorApp1.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly string _connectionString;

        public RoleService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<RoleDto?> GetByNameAsync(string roleName)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = "SELECT RoleId, RoleName FROM dbo.Roles WHERE RoleName = @RoleName";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RoleName", roleName);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // GetOrdinal retrieves the zero-based column index for the given column name
                int idOrdinal = reader.GetOrdinal("RoleId");
                int nameOrdinal = reader.GetOrdinal("RoleName");

                return new RoleDto
                {
                    RoleId = reader.GetInt32(idOrdinal),
                    RoleName = reader.GetString(nameOrdinal)
                };
            }
            
            return null;
        }

        public async Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = "SELECT RoleId, RoleName FROM dbo.Roles";
            
            using var cmd = new SqlCommand(sql, conn);
            
            var roles = new List<RoleDto>();
            using var reader = await cmd.ExecuteReaderAsync();

            // Cache ordinals outside the loop for better performance
            int idOrdinal = reader.GetOrdinal("RoleId");
            int nameOrdinal = reader.GetOrdinal("RoleName");

            while (await reader.ReadAsync())
            {
                roles.Add(new RoleDto
                {
                    RoleId = reader.GetInt32(idOrdinal),
                    RoleName = reader.GetString(nameOrdinal)
                });
            }
            
            return roles;
        }
    }
}
