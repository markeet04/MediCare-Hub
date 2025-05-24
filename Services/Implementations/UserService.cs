using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlazorApp1.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
        }

   // change signature:
public async Task<int> CreateUserAsync(UserDto userDto, string password, string roleName)
{
    var passwordHash = HashPassword(password);
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    using var tx = conn.BeginTransaction();
    try
    {
        // 1) Insert user
        const string insertUserSql = @"
          INSERT INTO dbo.Users 
            (UserName, PasswordHash, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt)
          VALUES 
            (@UserName, @PasswordHash, @CNIC, @PhoneNumber, @FullName, @ProfilePictureUrl, SYSUTCDATETIME(), SYSUTCDATETIME());
          SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var insertCmd = new SqlCommand(insertUserSql, conn, tx);
        insertCmd.Parameters.AddWithValue("@UserName", userDto.UserName);
        insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        insertCmd.Parameters.AddWithValue("@CNIC", userDto.CNIC);
        insertCmd.Parameters.AddWithValue("@PhoneNumber", (object?)userDto.PhoneNumber ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("@FullName", userDto.FullName);
        insertCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)userDto.ProfilePictureUrl ?? DBNull.Value);

        var rawId = await insertCmd.ExecuteScalarAsync();
        int userId = Convert.ToInt32(rawId);

        // 2) Lookup RoleId by name
        const string lookupRoleSql = "SELECT RoleId FROM dbo.Roles WHERE RoleName = @RoleName";
        using var lookupCmd = new SqlCommand(lookupRoleSql, conn, tx);
        lookupCmd.Parameters.AddWithValue("@RoleName", roleName);

        var roleObj = await lookupCmd.ExecuteScalarAsync();
        if (roleObj == null)
            throw new InvalidOperationException($"Role '{roleName}' not found.");

        int roleId = Convert.ToInt32(roleObj);

        // 3) Link user → role
        const string linkSql = "INSERT INTO dbo.UserRoles (UserId,RoleId) VALUES (@UserId,@RoleId)";
        using var linkCmd = new SqlCommand(linkSql, conn, tx);
        linkCmd.Parameters.AddWithValue("@UserId", userId);
        linkCmd.Parameters.AddWithValue("@RoleId", roleId);

        await linkCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return userId;
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}


        public async Task<UserDto?> GetByCnicAsync(string cnic)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                SELECT UserId, UserName, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt
                FROM dbo.Users
                WHERE CNIC = @CNIC";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CNIC", cnic);
            
            using var reader = await cmd.ExecuteReaderAsync();
         // Single-row mapping
if (await reader.ReadAsync())
{
    return new UserDto
    {
        UserId               = reader.GetInt32(reader.GetOrdinal("UserId")),
        UserName             = reader.GetString(reader.GetOrdinal("UserName")),
        CNIC                 = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber          = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                                  ? null
                                  : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        FullName             = reader.GetString(reader.GetOrdinal("FullName")),
        ProfilePictureUrl    = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl"))
                                  ? null
                                  : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
        CreatedAt            = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt            = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
    };
}

            
            return null;
        }

        public async Task DeleteUserAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = "DELETE FROM dbo.Users WHERE UserId = @UserId";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            
            await cmd.ExecuteNonQueryAsync();
        }
public async Task<string> GetUserRoleAsync(int userId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    var sql = @"
        SELECT r.RoleName
        FROM dbo.UserRoles ur
        JOIN dbo.Roles r ON ur.RoleId = r.RoleId
        WHERE ur.UserId = @UserId";
    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@UserId", userId);
    var result = await cmd.ExecuteScalarAsync();
    return result == null ? string.Empty : result.ToString();
}

        public async Task UpdateUserAsync(UserDto userDto)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                UPDATE dbo.Users 
                SET UserName = @UserName, 
                    CNIC = @CNIC, 
                    PhoneNumber = @PhoneNumber, 
                    FullName = @FullName, 
                    ProfilePictureUrl = @ProfilePictureUrl,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userDto.UserId);
            cmd.Parameters.AddWithValue("@UserName", userDto.UserName);
            cmd.Parameters.AddWithValue("@CNIC", userDto.CNIC);
            cmd.Parameters.AddWithValue("@PhoneNumber", (object?)userDto.PhoneNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FullName", userDto.FullName);
            cmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)userDto.ProfilePictureUrl ?? DBNull.Value);
            
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<UserDto>> GetAllByRoleAsync(string roleName)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            
            var sql = @"
                SELECT u.UserId, u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl, u.CreatedAt, u.UpdatedAt
                FROM dbo.Users u
                INNER JOIN dbo.UserRoles ur ON u.UserId = ur.UserId
                INNER JOIN dbo.Roles r ON ur.RoleId = r.RoleId
                WHERE r.RoleName = @RoleName";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RoleName", roleName);
            
            var users = new List<UserDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            
      // Multi-row mapping
while (await reader.ReadAsync())
{
    users.Add(new UserDto
    {
        UserId               = reader.GetInt32(reader.GetOrdinal("UserId")),
        UserName             = reader.GetString(reader.GetOrdinal("UserName")),
        CNIC                 = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber          = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                                  ? null
                                  : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        FullName             = reader.GetString(reader.GetOrdinal("FullName")),
        ProfilePictureUrl    = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl"))
                                  ? null
                                  : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
        CreatedAt            = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt            = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
    });
}

            
            return users;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}