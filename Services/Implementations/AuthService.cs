using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;

namespace BlazorApp1.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("DefaultConnection not found in configuration");
        }

        public async Task<UserDto?> ValidateCredentialsAsync(string userName, string password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    SELECT UserId, UserName, FullName, CNIC, PhoneNumber, ProfilePictureUrl, CreatedAt, UpdatedAt, PasswordHash
                    FROM Users 
                    WHERE UserName = @UserName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserName", userName);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var storedHash = reader["PasswordHash"].ToString();
                    var inputHash = HashPassword(password);

                    // Compare hashed passwords
                 if (storedHash == inputHash)
{
    return new UserDto
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        UserName = reader.GetString(reader.GetOrdinal("UserName")),
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
    };
}
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log exception here
                throw new Exception($"Error validating credentials: {ex.Message}", ex);
            }
        }

        public async Task<string?> GetUserRoleAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    SELECT r.RoleName 
                    FROM UserRoles ur
                    INNER JOIN Roles r ON ur.RoleId = r.RoleId
                    WHERE ur.UserId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                // Log exception here
                throw new Exception($"Error getting user role: {ex.Message}", ex);
            }
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string role)
        {
            try
            {
                var userRole = await GetUserRoleAsync(userId);
                return string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // Log exception here
                throw new Exception($"Error checking user role: {ex.Message}", ex);
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}