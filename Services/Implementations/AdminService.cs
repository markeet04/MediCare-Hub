using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlazorApp1.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly ILogger<AdminService> _logger;
        private readonly IWebHostEnvironment _environment;


        private readonly string _connectionString;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public AdminService(IConfiguration configuration, IUserService userService, IRoleService roleService, ILogger<AdminService> logger, IWebHostEnvironment environment)
        {

            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
            _userService = userService;
            _roleService = roleService;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
             _environment = environment;

        }
public async Task<AdminProfileDto> GetAdminByIdAsync(int adminId)
{
    try
    {
        var query = @"
            SELECT 
                u.UserId,
                u.FullName,
                u.UserName,
                u.PhoneNumber,
                u.ProfilePictureUrl,
                u.CreatedAt,
                u.UpdatedAt,
                ap.OfficeLocation
            FROM Users u
            INNER JOIN AdminProfiles ap ON u.UserId = ap.AdminId
            WHERE u.UserId = @AdminId";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@AdminId", adminId);

        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return new AdminProfileDto
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullName")),
                Username = reader.IsDBNull(reader.GetOrdinal("UserName")) ? string.Empty : reader.GetString(reader.GetOrdinal("UserName")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
                OfficeLocation = reader.IsDBNull(reader.GetOrdinal("OfficeLocation")) ? null : reader.GetString(reader.GetOrdinal("OfficeLocation")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        throw new InvalidOperationException($"Admin with ID {adminId} not found.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving admin profile for ID: {AdminId}", adminId);
        throw;
    }
}
public async Task LogActivityAsync(int adminId, string action, string details)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    const string sql = @"
        INSERT INTO dbo.ActivityLogs (AdminId, Action, Details, CreatedAt)
        VALUES (@AdminId, @Action, @Details, SYSUTCDATETIME());";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@AdminId", adminId);
    cmd.Parameters.AddWithValue("@Action", action);
    cmd.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);

    await cmd.ExecuteNonQueryAsync();
}


public async Task<DoctorProfileDto> GetDoctorByIdAsync(int doctorId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT 
            dp.DoctorId AS DoctorId,
            u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl,
            dp.Specialty, dp.Qualifications, dp.ConsultationFee
        FROM dbo.DoctorProfiles dp
        INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
        WHERE dp.DoctorId = @DoctorId
    ";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@DoctorId", doctorId);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return null!; // or throw if you prefer

    return new DoctorProfileDto
    {
        DoctorId         = reader.GetInt32(reader.GetOrdinal("DoctorId")),
        UserName         = reader.GetString(reader.GetOrdinal("UserName")),
        CNIC             = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber      = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        FullName         = reader.GetString(reader.GetOrdinal("FullName")),
        ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
        Specialty        = reader.IsDBNull(reader.GetOrdinal("Specialty"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Specialty")),
        Qualifications   = reader.IsDBNull(reader.GetOrdinal("Qualifications"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Qualifications")),
        ConsultationFee  = reader.IsDBNull(reader.GetOrdinal("ConsultationFee"))
                                ? null
                                : reader.GetDecimal(reader.GetOrdinal("ConsultationFee"))
    };
}

public async Task<PatientProfileDto> GetPatientByIdAsync(int patientId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT 
           pp.PatientId AS PatientId, u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl,
            pp.DateOfBirth, pp.Gender, pp.Address, pp.BloodGroup
        FROM dbo.Users u
        INNER JOIN dbo.PatientProfiles pp ON u.UserId = pp.PatientId
        WHERE u.UserId = @PatientId";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@PatientId", patientId);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return null!;

    return new PatientProfileDto
    {
        PatientId         = reader.GetInt32(reader.GetOrdinal("PatientId")),
        UserName       = reader.GetString(reader.GetOrdinal("UserName")),
        CNIC           = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber    = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        FullName       = reader.GetString(reader.GetOrdinal("FullName")),
        ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
        DateOfBirth    = reader.IsDBNull(reader.GetOrdinal("DateOfBirth"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
        Gender         = reader.IsDBNull(reader.GetOrdinal("Gender"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Gender")),
        Address        = reader.IsDBNull(reader.GetOrdinal("Address"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Address")),
        BloodGroup     = reader.IsDBNull(reader.GetOrdinal("BloodGroup"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("BloodGroup"))
    };
}

public async Task<ReceptionistProfileDto> GetReceptionistByIdAsync(int receptionistId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT 
           rp.ReceptionistId AS ReceptionistId, u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.Users u
        INNER JOIN dbo.ReceptionistProfiles rp ON u.UserId = rp.ReceptionistId
        WHERE u.UserId = @ReceptionistId";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@ReceptionistId", receptionistId);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return null!;

    return new ReceptionistProfileDto
    {
        ReceptionistId         = reader.GetInt32(reader.GetOrdinal("ReceptionistId")),
        UserName       = reader.GetString(reader.GetOrdinal("UserName")),
        CNIC           = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber    = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        FullName       = reader.GetString(reader.GetOrdinal("FullName")),
        ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
    };
}

public async Task<LabTechProfileDto> GetLabTechByIdAsync(int labTechId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT 
            ltp.LabTechId AS LabTechId, u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl,
            ltp.Qualifications
        FROM dbo.Users u
        INNER JOIN dbo.LabTechnicianProfiles ltp ON u.UserId = ltp.LabTechId
        WHERE u.UserId = @LabTechId";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@LabTechId", labTechId);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return null!;

    return new LabTechProfileDto
    {
        LabTechId         = reader.GetInt32(reader.GetOrdinal("LabTechId")),
        UserName       = reader.GetString(reader.GetOrdinal("UserName")),
        CNIC           = reader.GetString(reader.GetOrdinal("CNIC")),
        PhoneNumber    = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("PhoneNumber")),
        FullName       = reader.GetString(reader.GetOrdinal("FullName")),
        ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
        Qualifications = reader.IsDBNull(reader.GetOrdinal("Qualifications"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Qualifications"))
    };
}

        public async Task<(int userId, string tempPassword)> CreateDoctorAsync(CreateDoctorDto dto)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create user
                var userDto = new UserDto
                {
                    UserName = dto.UserName,
                    CNIC = dto.CNIC,
                    PhoneNumber = dto.PhoneNumber,
                    FullName = dto.FullName,
                    ProfilePictureUrl = dto.ProfilePictureUrl
                };

                var defaultPassword = dto.PhoneNumber ?? "default123";
                var passwordHash = HashPassword(defaultPassword);

                var userSql = @"
            INSERT INTO dbo.Users (UserName, PasswordHash, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt)
            VALUES (@UserName, @PasswordHash, @CNIC, @PhoneNumber, @FullName, @ProfilePictureUrl, SYSUTCDATETIME(), SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var userCmd = new SqlCommand(userSql, conn, transaction);
                userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
                userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
                userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
                userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);

                var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

                // Get Doctor role
                var roleSql = "SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Doctor'";
                using var roleCmd = new SqlCommand(roleSql, conn, transaction);
                var roleId = Convert.ToInt32(await roleCmd.ExecuteScalarAsync());

                // Assign role
                var userRoleSql = "INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
                using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
                userRoleCmd.Parameters.AddWithValue("@UserId", userId);
                userRoleCmd.Parameters.AddWithValue("@RoleId", roleId);
                await userRoleCmd.ExecuteNonQueryAsync();

                // Create doctor profile
                var profileSql = @"
            INSERT INTO dbo.DoctorProfiles (DoctorId, Specialty, Qualifications,ConsultationFee)
            VALUES (@DoctorId, @Specialty, @Qualifications, @ConsultationFee)";

                using var profileCmd = new SqlCommand(profileSql, conn, transaction);
                profileCmd.Parameters.AddWithValue("@DoctorId", userId);
                profileCmd.Parameters.AddWithValue("@Specialty", (object?)dto.Specialty ?? DBNull.Value);
                profileCmd.Parameters.AddWithValue("@Qualifications", (object?)dto.Qualifications ?? DBNull.Value);
                profileCmd.Parameters.AddWithValue("@ConsultationFee", (object?)dto.ConsultationFee ?? DBNull.Value);

                await profileCmd.ExecuteNonQueryAsync();

                transaction.Commit();

                return (userId, defaultPassword);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
    
}

public async Task<(int userId, string tempPassword)> CreateReceptionistAsync(CreateReceptionistDto dto)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        var defaultPassword = dto.PhoneNumber ?? "default123";
        var passwordHash = HashPassword(defaultPassword);

        var userSql = @"
            INSERT INTO dbo.Users (UserName, PasswordHash, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt)
            VALUES (@UserName, @PasswordHash, @CNIC, @PhoneNumber, @FullName, @ProfilePictureUrl, SYSUTCDATETIME(), SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
        userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
        userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
        userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);

        var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

        var roleSql = "SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Receptionist'";
        using var roleCmd = new SqlCommand(roleSql, conn, transaction);
        var roleId = Convert.ToInt32(await roleCmd.ExecuteScalarAsync());

        var userRoleSql = "INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@UserId", userId);
        userRoleCmd.Parameters.AddWithValue("@RoleId", roleId);
        await userRoleCmd.ExecuteNonQueryAsync();

        var profileSql = "INSERT INTO dbo.ReceptionistProfiles (ReceptionistId) VALUES (@ReceptionistId)";
        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@ReceptionistId", userId);
        await profileCmd.ExecuteNonQueryAsync();

        transaction.Commit();
        return (userId, defaultPassword);
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task<(int userId, string tempPassword)> CreatePatientAsync(CreatePatientDto dto)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        var defaultPassword = dto.PhoneNumber ?? "default123";
        var passwordHash = HashPassword(defaultPassword);

        var userSql = @"
            INSERT INTO dbo.Users (UserName, PasswordHash, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt)
            VALUES (@UserName, @PasswordHash, @CNIC, @PhoneNumber, @FullName, @ProfilePictureUrl, SYSUTCDATETIME(), SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
        userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
        userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
        userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);

        var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

        var roleSql = "SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Patient'";
        using var roleCmd = new SqlCommand(roleSql, conn, transaction);
        var roleId = Convert.ToInt32(await roleCmd.ExecuteScalarAsync());

        var userRoleSql = "INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@UserId", userId);
        userRoleCmd.Parameters.AddWithValue("@RoleId", roleId);
        await userRoleCmd.ExecuteNonQueryAsync();

        var profileSql = @"
            INSERT INTO dbo.PatientProfiles (PatientId, DateOfBirth, Gender, Address, BloodGroup)
            VALUES (@PatientId, @DateOfBirth, @Gender, @Address, @BloodGroup)";

        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@PatientId", userId);
        profileCmd.Parameters.AddWithValue("@DateOfBirth", (object?)dto.DateOfBirth ?? DBNull.Value);
        profileCmd.Parameters.AddWithValue("@Gender", (object?)dto.Gender ?? DBNull.Value);
        profileCmd.Parameters.AddWithValue("@Address", (object?)dto.Address ?? DBNull.Value);
        profileCmd.Parameters.AddWithValue("@BloodGroup", (object?)dto.BloodGroup ?? DBNull.Value);
        await profileCmd.ExecuteNonQueryAsync();

        transaction.Commit();
        return (userId, defaultPassword);
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task<(int userId, string tempPassword)> CreateLabTechAsync(CreateLabTechDto dto)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        var defaultPassword = dto.PhoneNumber ?? "default123";
        var passwordHash = HashPassword(defaultPassword);

        var userSql = @"
            INSERT INTO dbo.Users (UserName, PasswordHash, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt)
            VALUES (@UserName, @PasswordHash, @CNIC, @PhoneNumber, @FullName, @ProfilePictureUrl, SYSUTCDATETIME(), SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
        userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
        userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
        userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);

        var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

        var roleSql = "SELECT RoleId FROM dbo.Roles WHERE RoleName = 'LabTechnician'";
        using var roleCmd = new SqlCommand(roleSql, conn, transaction);
        var roleId = Convert.ToInt32(await roleCmd.ExecuteScalarAsync());

        var userRoleSql = "INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@UserId", userId);
        userRoleCmd.Parameters.AddWithValue("@RoleId", roleId);
        await userRoleCmd.ExecuteNonQueryAsync();

        var profileSql = @"
            INSERT INTO dbo.LabTechnicianProfiles (LabTechId, Qualifications)
            VALUES (@LabTechId, @Qualifications)";

        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@LabTechId", userId);
        profileCmd.Parameters.AddWithValue("@Qualifications", (object?)dto.Qualifications ?? DBNull.Value);
        await profileCmd.ExecuteNonQueryAsync();

        transaction.Commit();
        return (userId, defaultPassword);
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
public async Task UpdateAdminProfileAsync(EditAdminProfileDto dto)
{
    try
    {
        var query = @"
            UPDATE Users
            SET 
                FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                ProfilePictureUrl = @ProfilePictureUrl,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @AdminId;

            UPDATE AdminProfiles 
            SET 
                OfficeLocation = @OfficeLocation
            WHERE AdminId = @AdminId;";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@AdminId", dto.AdminId);
        command.Parameters.AddWithValue("@FullName", dto.FullName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProfilePictureUrl", dto.ProfilePictureUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfficeLocation", dto.OfficeLocation ?? (object)DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Admin profile with ID {dto.AdminId} not found or could not be updated.");
        }

        _logger.LogInformation("Admin profile updated successfully for ID: {AdminId}", dto.AdminId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating admin profile for ID: {AdminId}", dto.AdminId);
        throw;
    }
}

public async Task ChangePasswordAsync(EditAdminPasswordDto dto)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // First, verify the current password
        var verifyQuery = "SELECT PasswordHash FROM Users WHERE UserId = @AdminId";
        using var verifyCommand = new SqlCommand(verifyQuery, connection);
        verifyCommand.Parameters.AddWithValue("@AdminId", dto.AdminId);

        var currentHashObj = await verifyCommand.ExecuteScalarAsync();
        if (currentHashObj == null)
        {
            throw new InvalidOperationException("Admin not found.");
        }

        var currentHash = currentHashObj.ToString();
        if (!VerifyPassword(dto.CurrentPassword, currentHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // Hash the new password
        var newPasswordHash = HashPassword(dto.NewPassword);

        // Update the password
        var updateQuery = @"
            UPDATE Users 
            SET 
                PasswordHash = @NewPasswordHash,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @AdminId";

        using var updateCommand = new SqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@AdminId", dto.AdminId);
        updateCommand.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("Failed to update password.");
        }

        _logger.LogInformation("Password changed successfully for admin ID: {AdminId}", dto.AdminId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error changing password for admin ID: {AdminId}", dto.AdminId);
        throw;
    }
}
         private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
    

        // UPDATE METHODS

        public async Task UpdateDoctorAsync(UpdateDoctorDto dto)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Update Users table
                var userSql = @"
            UPDATE dbo.Users 
            SET UserName = @UserName,
                CNIC = @CNIC,
                PhoneNumber = @PhoneNumber,
                FullName = @FullName,
                ProfilePictureUrl = @ProfilePictureUrl,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @DoctorId";

                using var userCmd = new SqlCommand(userSql, conn, transaction);
                userCmd.Parameters.AddWithValue("@DoctorId", dto.DoctorId);
                userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
                userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
                userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
                userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);
                await userCmd.ExecuteNonQueryAsync();

                // Update DoctorProfiles table
                var profileSql = @"
            UPDATE dbo.DoctorProfiles 
            SET Specialty = @Specialty,
            Qualifications = @Qualifications,
            ConsultationFee = @ConsultationFee

            WHERE DoctorId = @DoctorId";

                using var profileCmd = new SqlCommand(profileSql, conn, transaction);
                profileCmd.Parameters.AddWithValue("@DoctorId", dto.DoctorId);
                profileCmd.Parameters.AddWithValue("@Specialty", (object?)dto.Specialty ?? DBNull.Value);
                profileCmd.Parameters.AddWithValue("@Qualifications", (object?)dto.Qualifications ?? DBNull.Value);
                profileCmd.Parameters.AddWithValue("@ConsultationFee", (object?)dto.ConsultationFee ?? DBNull.Value);

                await profileCmd.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

public async Task UpdateReceptionistAsync(UpdateReceptionistDto dto)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Update Users table
        var userSql = @"
            UPDATE dbo.Users 
            SET UserName = @UserName,
                CNIC = @CNIC,
                PhoneNumber = @PhoneNumber,
                FullName = @FullName,
                ProfilePictureUrl = @ProfilePictureUrl,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @ReceptionistId";

        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@ReceptionistId", dto.ReceptionistId);
        userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
        userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
        userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
        userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);
        await userCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task UpdatePatientAsync(UpdatePatientDto dto)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Update Users table
        var userSql = @"
            UPDATE dbo.Users 
            SET UserName = @UserName,
                CNIC = @CNIC,
                PhoneNumber = @PhoneNumber,
                FullName = @FullName,
                ProfilePictureUrl = @ProfilePictureUrl,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @PatientId";

        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@PatientId", dto.PatientId);
        userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
        userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
        userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
        userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);
        await userCmd.ExecuteNonQueryAsync();

        // Update PatientProfiles table
        var profileSql = @"
            UPDATE dbo.PatientProfiles 
            SET DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                Address = @Address,
                BloodGroup = @BloodGroup
            WHERE PatientId = @PatientId";

        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@PatientId", dto.PatientId);
        profileCmd.Parameters.AddWithValue("@DateOfBirth", (object?)dto.DateOfBirth ?? DBNull.Value);
        profileCmd.Parameters.AddWithValue("@Gender", (object?)dto.Gender ?? DBNull.Value);
        profileCmd.Parameters.AddWithValue("@Address", (object?)dto.Address ?? DBNull.Value);
        profileCmd.Parameters.AddWithValue("@BloodGroup", (object?)dto.BloodGroup ?? DBNull.Value);
        await profileCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task UpdateLabTechAsync(UpdateLabTechDto dto)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Update Users table
        var userSql = @"
            UPDATE dbo.Users 
            SET UserName = @UserName,
                CNIC = @CNIC,
                PhoneNumber = @PhoneNumber,
                FullName = @FullName,
                ProfilePictureUrl = @ProfilePictureUrl,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @LabTechId";

        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@LabTechId", dto.LabTechId);
        userCmd.Parameters.AddWithValue("@UserName", dto.UserName);
        userCmd.Parameters.AddWithValue("@CNIC", dto.CNIC);
        userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value);
        userCmd.Parameters.AddWithValue("@FullName", dto.FullName);
        userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)dto.ProfilePictureUrl ?? DBNull.Value);
        await userCmd.ExecuteNonQueryAsync();

        // Update LabTechnicianProfiles table
        var profileSql = @"
            UPDATE dbo.LabTechnicianProfiles 
            SET Qualifications = @Qualifications
            WHERE LabTechId = @LabTechId";

        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@LabTechId", dto.LabTechId);
        profileCmd.Parameters.AddWithValue("@Qualifications", (object?)dto.Qualifications ?? DBNull.Value);
        await profileCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

// DELETE METHODS

public async Task DeleteDoctorAsync(int doctorId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Check if doctor has any appointments
        var checkSql = "SELECT COUNT(*) FROM dbo.Appointments WHERE DoctorId = @DoctorId";
        using var checkCmd = new SqlCommand(checkSql, conn, transaction);
        checkCmd.Parameters.AddWithValue("@DoctorId", doctorId);
        var appointmentCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (appointmentCount > 0)
        {
            throw new InvalidOperationException("Cannot delete doctor with existing appointments. Please handle appointments first.");
        }

        // Delete doctor profile
        var profileSql = "DELETE FROM dbo.DoctorProfiles WHERE DoctorId = @DoctorId";
        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@DoctorId", doctorId);
        await profileCmd.ExecuteNonQueryAsync();

        // Delete user roles
        var userRoleSql = "DELETE FROM dbo.UserRoles WHERE UserId = @DoctorId";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@DoctorId", doctorId);
        await userRoleCmd.ExecuteNonQueryAsync();

        // Delete user
        var userSql = "DELETE FROM dbo.Users WHERE UserId = @DoctorId";
        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@DoctorId", doctorId);
        await userCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task DeleteReceptionistAsync(int receptionistId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Delete receptionist profile
        var profileSql = "DELETE FROM dbo.ReceptionistProfiles WHERE ReceptionistId = @ReceptionistId";
        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@ReceptionistId", receptionistId);
        await profileCmd.ExecuteNonQueryAsync();

        // Delete user roles
        var userRoleSql = "DELETE FROM dbo.UserRoles WHERE UserId = @ReceptionistId";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@ReceptionistId", receptionistId);
        await userRoleCmd.ExecuteNonQueryAsync();

        // Delete user
        var userSql = "DELETE FROM dbo.Users WHERE UserId = @ReceptionistId";
        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@ReceptionistId", receptionistId);
        await userCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task DeletePatientAsync(int patientId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Check if patient has any appointments
        var checkAppointmentsSql = "SELECT COUNT(*) FROM dbo.Appointments WHERE PatientId = @PatientId";
        using var checkAppointmentsCmd = new SqlCommand(checkAppointmentsSql, conn, transaction);
        checkAppointmentsCmd.Parameters.AddWithValue("@PatientId", patientId);
        var appointmentCount = Convert.ToInt32(await checkAppointmentsCmd.ExecuteScalarAsync());

        // Check if patient has any lab orders
        var checkLabOrdersSql = "SELECT COUNT(*) FROM dbo.LabOrders WHERE PatientId = @PatientId";
        using var checkLabOrdersCmd = new SqlCommand(checkLabOrdersSql, conn, transaction);
        checkLabOrdersCmd.Parameters.AddWithValue("@PatientId", patientId);
        var labOrderCount = Convert.ToInt32(await checkLabOrdersCmd.ExecuteScalarAsync());

        if (appointmentCount > 0 || labOrderCount > 0)
        {
            throw new InvalidOperationException("Cannot delete patient with existing appointments or lab orders. Please handle these records first.");
        }

        // Delete patient profile
        var profileSql = "DELETE FROM dbo.PatientProfiles WHERE PatientId = @PatientId";
        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@PatientId", patientId);
        await profileCmd.ExecuteNonQueryAsync();

        // Delete user roles
        var userRoleSql = "DELETE FROM dbo.UserRoles WHERE UserId = @PatientId";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@PatientId", patientId);
        await userRoleCmd.ExecuteNonQueryAsync();

        // Delete user
        var userSql = "DELETE FROM dbo.Users WHERE UserId = @PatientId";
        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@PatientId", patientId);
        await userCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

public async Task DeleteLabTechAsync(int labTechId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Check if lab tech has any lab orders assigned
        var checkSql = "SELECT COUNT(*) FROM dbo.LabOrders WHERE LabTechId = @LabTechId";
        using var checkCmd = new SqlCommand(checkSql, conn, transaction);
        checkCmd.Parameters.AddWithValue("@LabTechId", labTechId);
        var labOrderCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (labOrderCount > 0)
        {
            throw new InvalidOperationException("Cannot delete lab technician with assigned lab orders. Please reassign or complete lab orders first.");
        }

        // Delete lab tech profile
        var profileSql = "DELETE FROM dbo.LabTechnicianProfiles WHERE LabTechId = @LabTechId";
        using var profileCmd = new SqlCommand(profileSql, conn, transaction);
        profileCmd.Parameters.AddWithValue("@LabTechId", labTechId);
        await profileCmd.ExecuteNonQueryAsync();

        // Delete user roles
        var userRoleSql = "DELETE FROM dbo.UserRoles WHERE UserId = @LabTechId";
        using var userRoleCmd = new SqlCommand(userRoleSql, conn, transaction);
        userRoleCmd.Parameters.AddWithValue("@LabTechId", labTechId);
        await userRoleCmd.ExecuteNonQueryAsync();

        // Delete user
        var userSql = "DELETE FROM dbo.Users WHERE UserId = @LabTechId";
        using var userCmd = new SqlCommand(userSql, conn, transaction);
        userCmd.Parameters.AddWithValue("@LabTechId", labTechId);
        await userCmd.ExecuteNonQueryAsync();

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

// ADDITIONAL GET METHODS FOR LISTING/VIEWING USERS

public async Task<IEnumerable<DoctorProfileDto>> GetAllDoctorsAsync()
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT dp.DoctorId, dp.Specialty, dp.Qualifications,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.DoctorProfiles dp
        INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
        ORDER BY u.FullName";

    using var cmd = new SqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    var doctors = new List<DoctorProfileDto>();
    while (await reader.ReadAsync())
    {
        doctors.Add(new DoctorProfileDto
        {
            DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
            Specialty = reader.IsDBNull(reader.GetOrdinal("Specialty")) ? null : reader.GetString(reader.GetOrdinal("Specialty")),
            Qualifications = reader.IsDBNull(reader.GetOrdinal("Qualifications")) ? null : reader.GetString(reader.GetOrdinal("Qualifications")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
        });
    }

    return doctors;
}

public async Task<IEnumerable<ReceptionistProfileDto>> GetAllReceptionistsAsync()
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT rp.ReceptionistId,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.ReceptionistProfiles rp
        INNER JOIN dbo.Users u ON rp.ReceptionistId = u.UserId
        ORDER BY u.FullName";

    using var cmd = new SqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    var receptionists = new List<ReceptionistProfileDto>();
    while (await reader.ReadAsync())
    {
        receptionists.Add(new ReceptionistProfileDto
        {
            ReceptionistId = reader.GetInt32(reader.GetOrdinal("ReceptionistId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
        });
    }

    return receptionists;
}

public async Task<IEnumerable<PatientProfileDto>> GetAllPatientsAsync()
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT pp.PatientId, pp.DateOfBirth, pp.Gender, pp.Address, pp.BloodGroup,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.PatientProfiles pp
        INNER JOIN dbo.Users u ON pp.PatientId = u.UserId
        ORDER BY u.FullName";

    using var cmd = new SqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    var patients = new List<PatientProfileDto>();
    while (await reader.ReadAsync())
    {
        patients.Add(new PatientProfileDto
        {
            PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
            DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : reader.GetString(reader.GetOrdinal("Gender")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
            BloodGroup = reader.IsDBNull(reader.GetOrdinal("BloodGroup")) ? null : reader.GetString(reader.GetOrdinal("BloodGroup")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
        });
    }

    return patients;
}

public async Task<IEnumerable<LabTechProfileDto>> GetAllLabTechsAsync()
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT ltp.LabTechId, ltp.Qualifications,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.LabTechnicianProfiles ltp
        INNER JOIN dbo.Users u ON ltp.LabTechId = u.UserId
        ORDER BY u.FullName";

    using var cmd = new SqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    var labTechs = new List<LabTechProfileDto>();
    while (await reader.ReadAsync())
    {
        labTechs.Add(new LabTechProfileDto
        {
            LabTechId = reader.GetInt32(reader.GetOrdinal("LabTechId")),
            Qualifications = reader.IsDBNull(reader.GetOrdinal("Qualifications")) ? null : reader.GetString(reader.GetOrdinal("Qualifications")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
        });
    }

    return labTechs;
}

       public async Task<GlobalStatsDto> GetGlobalStatsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
        -- Count users by role
        SELECT 
            SUM(CASE WHEN r.RoleName = 'Admin' THEN 1 ELSE 0 END) AS TotalAdmins,
            SUM(CASE WHEN r.RoleName = 'Doctor' THEN 1 ELSE 0 END) AS TotalDoctors,
            SUM(CASE WHEN r.RoleName = 'Receptionist' THEN 1 ELSE 0 END) AS TotalReceptionists,
            SUM(CASE WHEN r.RoleName = 'Patient' THEN 1 ELSE 0 END) AS TotalPatients,
            SUM(CASE WHEN r.RoleName = 'LabTechnician' THEN 1 ELSE 0 END) AS TotalLabTechnicians
        FROM dbo.Users u
        INNER JOIN dbo.UserRoles ur ON u.UserId = ur.UserId
        INNER JOIN dbo.Roles r ON ur.RoleId = r.RoleId;

        -- Count appointments by status
        SELECT 
            SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingAppointments,
            SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END) AS ApprovedAppointments,
            SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledAppointments,
            SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedAppointments
        FROM dbo.Appointments;

        -- Count lab orders by status
        SELECT 
            SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingLabOrders,
            SUM(CASE WHEN Status = 'InProgress' THEN 1 ELSE 0 END) AS InProgressLabOrders,
            SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedLabOrders,
            SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledLabOrders
        FROM dbo.LabOrders;";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var stats = new GlobalStatsDto();

            // Read user counts (First result set)
            if (await reader.ReadAsync())
            {
                stats.TotalAdmins = reader.IsDBNull(reader.GetOrdinal("TotalAdmins")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalAdmins"));
                stats.TotalDoctors = reader.IsDBNull(reader.GetOrdinal("TotalDoctors")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalDoctors"));
                stats.TotalReceptionists = reader.IsDBNull(reader.GetOrdinal("TotalReceptionists")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalReceptionists"));
                stats.TotalPatients = reader.IsDBNull(reader.GetOrdinal("TotalPatients")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalPatients"));
                stats.TotalLabTechnicians = reader.IsDBNull(reader.GetOrdinal("TotalLabTechnicians")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalLabTechnicians"));
            }

            // Read appointment counts (Second result set)
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                stats.PendingAppointments = reader.IsDBNull(reader.GetOrdinal("PendingAppointments")) ? 0 : reader.GetInt32(reader.GetOrdinal("PendingAppointments"));
                stats.ApprovedAppointments = reader.IsDBNull(reader.GetOrdinal("ApprovedAppointments")) ? 0 : reader.GetInt32(reader.GetOrdinal("ApprovedAppointments"));
                stats.CancelledAppointments = reader.IsDBNull(reader.GetOrdinal("CancelledAppointments")) ? 0 : reader.GetInt32(reader.GetOrdinal("CancelledAppointments"));
                stats.CompletedAppointments = reader.IsDBNull(reader.GetOrdinal("CompletedAppointments")) ? 0 : reader.GetInt32(reader.GetOrdinal("CompletedAppointments"));
            }

            // Read lab order counts (Third result set)
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                stats.PendingLabOrders = reader.IsDBNull(reader.GetOrdinal("PendingLabOrders")) ? 0 : reader.GetInt32(reader.GetOrdinal("PendingLabOrders"));
                stats.InProgressLabOrders = reader.IsDBNull(reader.GetOrdinal("InProgressLabOrders")) ? 0 : reader.GetInt32(reader.GetOrdinal("InProgressLabOrders"));
                stats.CompletedLabOrders = reader.IsDBNull(reader.GetOrdinal("CompletedLabOrders")) ? 0 : reader.GetInt32(reader.GetOrdinal("CompletedLabOrders"));
                stats.CancelledLabOrders = reader.IsDBNull(reader.GetOrdinal("CancelledLabOrders")) ? 0 : reader.GetInt32(reader.GetOrdinal("CancelledLabOrders"));
            }

            return stats;
        }

        public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT al.LogId, al.AdminId, al.Action, al.Details, al.CreatedAt, u.FullName as AdminName
                FROM dbo.ActivityLogs al
                INNER JOIN dbo.Users u ON al.AdminId = u.UserId
                ORDER BY al.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var logs = new List<ActivityLogDto>();
            while (await reader.ReadAsync())
            {
                logs.Add(new ActivityLogDto
                {
                    LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                    AdminId = reader.GetInt32(reader.GetOrdinal("AdminId")),
                    Action = reader.GetString(reader.GetOrdinal("Action")),
                    Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? null : reader.GetString(reader.GetOrdinal("Details")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    AdminName = reader.GetString(reader.GetOrdinal("AdminName"))
                });
            }

            return logs;
        }



// inside your AdminService class
public async Task<int> GenerateReportAsync(GenerateReportRequestDto dto)
{
    // 1) Prepare file paths
    var fileName      = $"{dto.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
    var wwwRoot       = _environment.WebRootPath;
    var reportsFolder = Path.Combine(wwwRoot, "reports");
    if (!Directory.Exists(reportsFolder))
        Directory.CreateDirectory(reportsFolder);
    var diskPath = Path.Combine(reportsFolder, fileName);

    int reportId;
    using (var conn = new SqlConnection(_connectionString))
    {
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        // 2) Gather all report data based on type, date range, filters
        var data = await GatherReportDataAsync(conn, tx, dto);

        // 3) Generate the PDF using your real data
        await GeneratePdfReportAsync(dto.ReportType, data, diskPath, dto);

        // 4) Insert a record into dbo.Reports with the real file path
        var webPath = $"/reports/{fileName}";
        const string sql = @"
            INSERT INTO dbo.Reports 
                (AdminId, ReportType, Parameters, FilePath, CreatedAt)
            VALUES 
                (@AdminId, @ReportType, @Parameters, @FilePath, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@AdminId", dto.AdminId);
        cmd.Parameters.AddWithValue("@ReportType", dto.ReportType);
        cmd.Parameters.AddWithValue("@Parameters", dto.AdditionalFilters ?? string.Empty);
        cmd.Parameters.AddWithValue("@FilePath", webPath);

        reportId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // 5) Log the creation activity
        await LogActivityAsync(
            dto.AdminId,
            "Generated Report",
            $"ReportType={dto.ReportType}, Start={dto.StartDate:yyyy-MM-dd}, End={dto.EndDate:yyyy-MM-dd}"
        );

        await tx.CommitAsync();
    }

    return reportId;
}


// Example placeholder for data-fetching
private async Task<List<YourReportRowDto>> FetchReportDataAsync(GenerateReportRequestDto dto)
{
    // TODO: replace with real ADO.NET queries based on dto parameters
    await Task.Delay(50);
    return new List<YourReportRowDto>
    {
        new YourReportRowDto { FieldA = "Row1A", FieldB = "Row1B", FieldC = "Row1C" },
        new YourReportRowDto { FieldA = "Row2A", FieldB = "Row2B", FieldC = "Row2C" }
    };
}

// Example DTO for report rows
public class YourReportRowDto
{
    public string FieldA { get; set; } = "";
    public string FieldB { get; set; } = "";
    public string FieldC { get; set; } = "";
}

         private async Task<Dictionary<string, object>> GatherReportDataAsync(
            SqlConnection conn, 
            SqlTransaction transaction, 
            GenerateReportRequestDto dto)
        {
            var data = new Dictionary<string, object>();

            switch (dto.ReportType.ToLowerInvariant())
            {
                case "user-statistics":
                    data = await GatherUserStatisticsReportDataAsync(conn, transaction, dto);
                    break;
                case "patient-statistics":
                    data = await GatherPatientStatisticsReportDataAsync(conn, transaction, dto);
                    break;
                case "appointments":
                    data = await GatherAppointmentsReportDataAsync(conn, transaction, dto);
                    break;
                case "lab-orders":
                    data = await GatherLabOrdersReportDataAsync(conn, transaction, dto);
                    break;
                default:
                    throw new ArgumentException($"Unsupported report type: {dto.ReportType}. Supported types: user-statistics, patient-statistics, appointments, lab-orders");
            }

            data["ReportType"] = dto.ReportType;
            data["GeneratedAt"] = DateTime.UtcNow;
            data["StartDate"] = dto.StartDate;
            data["EndDate"] = dto.EndDate;
            data["AdditionalFilters"] = dto.AdditionalFilters;

            return data;
        }

        private async Task<Dictionary<string, object>> GatherUserStatisticsReportDataAsync(
            SqlConnection conn, SqlTransaction transaction, GenerateReportRequestDto dto)
        {
            var stats = new Dictionary<string, object>();

            // Get user counts by role
            var userCountSql = @"
                SELECT 
                    (SELECT COUNT(*) FROM dbo.PatientProfiles) as TotalPatients,
                    (SELECT COUNT(*) FROM dbo.DoctorProfiles) as TotalDoctors,
                    (SELECT COUNT(*) FROM dbo.ReceptionistProfiles) as TotalReceptionists,
                    (SELECT COUNT(*) FROM dbo.LabTechnicianProfiles) as TotalLabTechs,
                    (SELECT COUNT(*) FROM dbo.AdminProfiles) as TotalAdmins";

            using var cmd = new SqlCommand(userCountSql, conn, transaction);
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                stats["TotalPatients"] = reader["TotalPatients"];
                stats["TotalDoctors"] = reader["TotalDoctors"];
                stats["TotalReceptionists"] = reader["TotalReceptionists"];
                stats["TotalLabTechs"] = reader["TotalLabTechs"];
                stats["TotalAdmins"] = reader["TotalAdmins"];
            }
            reader.Close();

            // Get recent registrations if date range specified
            if (dto.StartDate.HasValue || dto.EndDate.HasValue)
            {
                var dateFilter = BuildDateFilter("u.CreatedAt", dto.StartDate, dto.EndDate);
                
                var recentUsersSql = $@"
                    SELECT 
                        r.RoleName,
                        COUNT(*) as RecentCount
                    FROM dbo.Users u
                    INNER JOIN dbo.Roles r ON u.UserId = r.RoleId
                    {dateFilter}
                    GROUP BY r.RoleName";

                using var recentCmd = new SqlCommand(recentUsersSql, conn, transaction);
                AddDateParameters(recentCmd, dto.StartDate, dto.EndDate);
                
                var recentUsers = new List<Dictionary<string, object>>();
                using var recentReader = await recentCmd.ExecuteReaderAsync();
                
                while (await recentReader.ReadAsync())
                {
                    recentUsers.Add(new Dictionary<string, object>
                    {
                        ["RoleName"] = recentReader["RoleName"],
                        ["RecentCount"] = recentReader["RecentCount"]
                    });
                }
                
                stats["RecentRegistrations"] = recentUsers;
            }

            return stats;
        }

      // ...existing code...
private async Task<Dictionary<string, object>> GatherPatientStatisticsReportDataAsync(
    SqlConnection conn, SqlTransaction transaction, GenerateReportRequestDto dto)
{
    var data = new Dictionary<string, object>();
    var dateFilter = BuildDateFilter("u.CreatedAt", dto.StartDate, dto.EndDate);

    // Get patient details (fixed columns and join)
    var patientsSql = $@"
        SELECT 
            p.PatientId, 
            u.FullName, 
            u.UserName, 
            u.CNIC, 
            u.PhoneNumber, 
            u.ProfilePictureUrl,
            p.DateOfBirth, 
            p.Gender, 
            p.Address, 
            p.BloodGroup,
            u.CreatedAt as RegistrationDate
        FROM dbo.PatientProfiles p
        INNER JOIN dbo.Users u ON p.PatientId = u.UserId
        {dateFilter}
        ORDER BY u.CreatedAt DESC";

    using var cmd = new SqlCommand(patientsSql, conn, transaction);
    AddDateParameters(cmd, dto.StartDate, dto.EndDate);

    var patients = new List<Dictionary<string, object>>();
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var patient = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            patient[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        patients.Add(patient);
    }
    reader.Close();

    // Get gender distribution (no change needed)
    var genderSql = $@"
        SELECT 
            p.Gender,
            COUNT(*) as Count
        FROM dbo.PatientProfiles p
        INNER JOIN dbo.Users u ON p.PatientId = u.UserId
        {dateFilter}
        GROUP BY p.Gender";

    using var genderCmd = new SqlCommand(genderSql, conn, transaction);
    AddDateParameters(genderCmd, dto.StartDate, dto.EndDate);

    var genderStats = new List<Dictionary<string, object>>();
    using var genderReader = await genderCmd.ExecuteReaderAsync();

    while (await genderReader.ReadAsync())
    {
        genderStats.Add(new Dictionary<string, object>
        {
            ["Gender"] = genderReader["Gender"]?.ToString() ?? "Unknown",
            ["Count"] = genderReader["Count"]
        });
    }

    data["Patients"] = patients;
    data["TotalCount"] = patients.Count;
    data["GenderDistribution"] = genderStats;

    return data;
}


   private async Task<Dictionary<string, object>> GatherAppointmentsReportDataAsync(
    SqlConnection conn, SqlTransaction transaction, GenerateReportRequestDto dto)
{
    var dateFilter = BuildDateFilter("a.ScheduledDateTime", dto.StartDate, dto.EndDate);

    var appointmentsSql = $@"
        SELECT 
            a.AppointmentId, 
            a.ScheduledDateTime, 
            a.Status, 
            -- a.Notes, -- Remove this if not in your schema
            up.FullName AS PatientName,
            ud.FullName AS DoctorName,
            dp.Specialty AS Specialization
        FROM dbo.Appointments a
        INNER JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        INNER JOIN dbo.Users up ON pp.PatientId = up.UserId
        INNER JOIN dbo.DoctorProfiles dp ON a.DoctorId = dp.DoctorId
        INNER JOIN dbo.Users ud ON dp.DoctorId = ud.UserId
        {dateFilter}
        ORDER BY a.ScheduledDateTime DESC";

    using var cmd = new SqlCommand(appointmentsSql, conn, transaction);
    AddDateParameters(cmd, dto.StartDate, dto.EndDate);

    var appointments = new List<Dictionary<string, object>>();
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var appointment = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            appointment[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        appointments.Add(appointment);
    }
    reader.Close();

    // Get status distribution
    var statusSql = $@"
        SELECT 
            a.Status,
            COUNT(*) as Count
        FROM dbo.Appointments a
        {dateFilter}
        GROUP BY a.Status";

    using var statusCmd = new SqlCommand(statusSql, conn, transaction);
    AddDateParameters(statusCmd, dto.StartDate, dto.EndDate);

    var statusStats = new List<Dictionary<string, object>>();
    using var statusReader = await statusCmd.ExecuteReaderAsync();

    while (await statusReader.ReadAsync())
    {
        statusStats.Add(new Dictionary<string, object>
        {
            ["Status"] = statusReader["Status"]?.ToString() ?? "Unknown",
            ["Count"] = statusReader["Count"]
        });
    }

    return new Dictionary<string, object>
    {
        ["Appointments"] = appointments,
        ["TotalCount"] = appointments.Count,
        ["StatusDistribution"] = statusStats
    };
}

   private async Task<Dictionary<string, object>> GatherLabOrdersReportDataAsync(
    SqlConnection conn, SqlTransaction transaction, GenerateReportRequestDto dto)
{
    var dateFilter = BuildDateFilter("lo.OrderDate", dto.StartDate, dto.EndDate);

    var labOrdersSql = $@"
        SELECT 
            lo.LabOrderId, lo.Status, lo.OrderDate,
            lo.Results, lo.ReportFilePath, lo.CompletedAt,
            up.FullName AS PatientName,
            ut.FullName AS LabTechName
        FROM dbo.LabOrders lo
        INNER JOIN dbo.PatientProfiles pp ON lo.PatientId = pp.PatientId
        INNER JOIN dbo.Users up ON pp.PatientId = up.UserId
        LEFT JOIN dbo.LabTechnicianProfiles ltp ON lo.LabTechId = ltp.LabTechId
        LEFT JOIN dbo.Users ut ON ltp.LabTechId = ut.UserId
        {dateFilter}
        ORDER BY lo.OrderDate DESC";

    using var cmd = new SqlCommand(labOrdersSql, conn, transaction);
    AddDateParameters(cmd, dto.StartDate, dto.EndDate);

    var labOrders = new List<Dictionary<string, object>>();
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var result = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            result[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        labOrders.Add(result);
    }
    reader.Close();

    // Example: Group by Status instead of TestName
    var testTypeSql = $@"
        SELECT 
            lo.Status,
            COUNT(*) as Count
        FROM dbo.LabOrders lo
        {dateFilter}
        GROUP BY lo.Status
        ORDER BY COUNT(*) DESC";

    using var testCmd = new SqlCommand(testTypeSql, conn, transaction);
    AddDateParameters(testCmd, dto.StartDate, dto.EndDate);

    var testStats = new List<Dictionary<string, object>>();
    using var testReader = await testCmd.ExecuteReaderAsync();

    while (await testReader.ReadAsync())
    {
        testStats.Add(new Dictionary<string, object>
        {
            ["Status"] = testReader["Status"]?.ToString() ?? "Unknown",
            ["Count"] = testReader["Count"]
        });
    }

    return new Dictionary<string, object>
    {
        ["LabOrders"] = labOrders,
        ["TotalCount"] = labOrders.Count,
        ["TestTypeDistribution"] = testStats
    };
}

        private string BuildDateFilter(string dateColumn, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue && !endDate.HasValue)
                return "";

            if (startDate.HasValue && endDate.HasValue)
                return $"WHERE {dateColumn} BETWEEN @StartDate AND @EndDate";
            
            if (startDate.HasValue)
                return $"WHERE {dateColumn} >= @StartDate";
            
            return $"WHERE {dateColumn} <= @EndDate";
        }

        private void AddDateParameters(SqlCommand cmd, DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue)
                cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
            if (endDate.HasValue)
                cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
        }

        private async Task GeneratePdfReportAsync(string reportType, Dictionary<string, object> data, string filePath, GenerateReportRequestDto dto)
        {
            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .Text($"Hospital Management System - {FormatReportTitle(reportType)} Report")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                        {
                            // Report metadata
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(10);
                                    if (dto.StartDate.HasValue)
                                        column.Item().Text($"Start Date: {dto.StartDate:yyyy-MM-dd}").FontSize(10);
                                    if (dto.EndDate.HasValue)
                                        column.Item().Text($"End Date: {dto.EndDate:yyyy-MM-dd}").FontSize(10);
                                });
                            });

                            col.Item().PaddingVertical(0.5f, Unit.Centimetre);

                            // Report content based on type
                            switch (reportType.ToLowerInvariant())
                            {
                                case "user-statistics":
                                    GenerateUserStatisticsContent(col, data);
                                    break;
                                case "patient-statistics":
                                    GeneratePatientStatisticsContent(col, data);
                                    break;
                                case "appointments":
                                    GenerateAppointmentsContent(col, data);
                                    break;
                                case "lab-orders":
                                    GenerateLabOrdersContent(col, data);
                                    break;
                            }
                        });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                }).GeneratePdf(filePath);
            });
        }

        private string FormatReportTitle(string reportType)
        {
            return reportType.ToLowerInvariant() switch
            {
                "user-statistics" => "User Statistics",
                "patient-statistics" => "Patient Statistics",
                "appointments" => "Appointments",
                "lab-orders" => "Lab Orders",
                _ => reportType.ToUpperInvariant()
            };
        }

        private void GenerateUserStatisticsContent(ColumnDescriptor col, Dictionary<string, object> data)
        {
            col.Item().Text("System User Overview").SemiBold().FontSize(16);
            col.Item().PaddingVertical(0.25f, Unit.Centimetre);

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Total Users by Role").SemiBold().FontSize(14);
                    column.Item().Text($"Patients: {data["TotalPatients"]}").FontSize(12);
                    column.Item().Text($"Doctors: {data["TotalDoctors"]}").FontSize(12);
                    column.Item().Text($"Receptionists: {data["TotalReceptionists"]}").FontSize(12);
                    column.Item().Text($"Lab Technicians: {data["TotalLabTechs"]}").FontSize(12);
                    column.Item().Text($"Administrators: {data["TotalAdmins"]}").FontSize(12);
                });
            });

            if (data.ContainsKey("RecentRegistrations"))
            {
                var recentUsers = (List<Dictionary<string, object>>)data["RecentRegistrations"];
                if (recentUsers.Any())
                {
                    col.Item().PaddingVertical(0.5f, Unit.Centimetre);
                    col.Item().Text("Recent Registrations (in selected period)").SemiBold().FontSize(14);
                    
                    foreach (var user in recentUsers)
                    {
                        col.Item().Text($"{user["RoleName"]}: {user["RecentCount"]} new registrations").FontSize(12);
                    }
                }
            }
        }

      // ...existing code...
private void GeneratePatientStatisticsContent(ColumnDescriptor col, Dictionary<string, object> data)
{
    var patients = (List<Dictionary<string, object>>)data["Patients"];
    var genderStats = (List<Dictionary<string, object>>)data["GenderDistribution"];
    
    col.Item().Text($"Patient Statistics - Total: {data["TotalCount"]}").SemiBold().FontSize(16);
    col.Item().PaddingVertical(0.25f, Unit.Centimetre);

    // Gender distribution
    if (genderStats.Any())
    {
        col.Item().Text("Gender Distribution").SemiBold().FontSize(14);
        foreach (var stat in genderStats)
        {
            col.Item().Text($"{stat["Gender"]}: {stat["Count"]} patients").FontSize(12);
        }
        col.Item().PaddingVertical(0.5f, Unit.Centimetre);
    }

    // Patient list
    if (patients.Any())
    {
        col.Item().Text("Patient List").SemiBold().FontSize(14);
        col.Item().PaddingVertical(0.25f, Unit.Centimetre);

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Name
                columns.RelativeColumn(2); // UserName
                columns.RelativeColumn(1); // Gender
                columns.RelativeColumn(1); // Age
                columns.RelativeColumn(1); // Registration
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Name").SemiBold();
                header.Cell().Element(CellStyle).Text("UserName").SemiBold();
                header.Cell().Element(CellStyle).Text("Gender").SemiBold();
                header.Cell().Element(CellStyle).Text("Age").SemiBold();
                header.Cell().Element(CellStyle).Text("Registered").SemiBold();
            });

            foreach (var patient in patients.Take(50))
            {
                var age = patient["DateOfBirth"] != null 
                    ? DateTime.Now.Year - ((DateTime)patient["DateOfBirth"]).Year 
                    : 0;

                table.Cell().Element(CellStyle).Text(patient["FullName"]?.ToString() ?? "").FontSize(10);
                table.Cell().Element(CellStyle).Text(patient["UserName"]?.ToString() ?? "").FontSize(10);
                table.Cell().Element(CellStyle).Text(patient["Gender"]?.ToString() ?? "").FontSize(10);
                table.Cell().Element(CellStyle).Text(age > 0 ? age.ToString() : "").FontSize(10);
                table.Cell().Element(CellStyle).Text(patient["RegistrationDate"] != null ? ((DateTime)patient["RegistrationDate"]).ToString("yyyy-MM-dd") : "").FontSize(10);
            }
        });

        if (patients.Count > 50)
        {
            col.Item().PaddingTop(0.5f, Unit.Centimetre).Text($"Note: Showing first 50 of {patients.Count} patients").FontSize(10).Italic();
        }
    }
}
// ...existing code...

        private void GenerateAppointmentsContent(ColumnDescriptor col, Dictionary<string, object> data)
        {
            var appointments = (List<Dictionary<string, object>>)data["Appointments"];
            var statusStats = (List<Dictionary<string, object>>)data["StatusDistribution"];
            
            col.Item().Text($"Appointments Report - Total: {data["TotalCount"]}").SemiBold().FontSize(16);
            col.Item().PaddingVertical(0.25f, Unit.Centimetre);

            // Status distribution
            if (statusStats.Any())
            {
                col.Item().Text("Status Distribution").SemiBold().FontSize(14);
                foreach (var stat in statusStats)
                {
                    col.Item().Text($"{stat["Status"]}: {stat["Count"]} appointments").FontSize(12);
                }
                col.Item().PaddingVertical(0.5f, Unit.Centimetre);
            }

            // Appointments list
            if (appointments.Any())
            {
                col.Item().Text("Appointment Details").SemiBold().FontSize(14);
                col.Item().PaddingVertical(0.25f, Unit.Centimetre);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1); // Date
                        columns.RelativeColumn(2); // Patient
                        columns.RelativeColumn(2); // Doctor
                        columns.RelativeColumn(1); // Status
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Date").SemiBold();
                        header.Cell().Element(CellStyle).Text("Patient").SemiBold();
                        header.Cell().Element(CellStyle).Text("Doctor").SemiBold();
                        header.Cell().Element(CellStyle).Text("Status").SemiBold();
                    });

                 // ...existing code...
foreach (var appointment in appointments.Take(100))
{
    table.Cell().Element(CellStyle).Text(
        appointment["ScheduledDateTime"] != null
            ? ((DateTime)appointment["ScheduledDateTime"]).ToString("yyyy-MM-dd HH:mm")
            : ""
    ).FontSize(10);
    table.Cell().Element(CellStyle).Text(appointment["PatientName"]?.ToString() ?? "").FontSize(10);
    table.Cell().Element(CellStyle).Text($"{appointment["DoctorName"]} ({appointment["Specialization"]})").FontSize(10);
    table.Cell().Element(CellStyle).Text(appointment["Status"]?.ToString() ?? "").FontSize(10);
}
// ...existing code...
                });

                if (appointments.Count > 100)
                {
                    col.Item().PaddingTop(0.5f, Unit.Centimetre).Text($"Note: Showing first 100 of {appointments.Count} appointments").FontSize(10).Italic();
                }
            }
        }

  private void GenerateLabOrdersContent(ColumnDescriptor col, Dictionary<string, object> data)
{
    var labOrders = (List<Dictionary<string, object>>)data["LabOrders"];
    var testStats = (List<Dictionary<string, object>>)data["TestTypeDistribution"];
    
    col.Item().Text($"Lab Orders Report - Total: {data["TotalCount"]}").SemiBold().FontSize(16);
    col.Item().PaddingVertical(0.25f, Unit.Centimetre);

    // Status distribution (instead of test type)
    if (testStats.Any())
    {
        col.Item().Text("Lab Orders by Status").SemiBold().FontSize(14);
        foreach (var stat in testStats.Take(10))
        {
            col.Item().Text($"{stat["Status"]}: {stat["Count"]} orders").FontSize(12);
        }
        col.Item().PaddingVertical(0.5f, Unit.Centimetre);
    }

    // Lab orders list
    if (labOrders.Any())
    {
        col.Item().Text("Lab Order Details").SemiBold().FontSize(14);
        col.Item().PaddingVertical(0.25f, Unit.Centimetre);

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1); // Date
                columns.RelativeColumn(2); // Patient
                columns.RelativeColumn(2); // Lab Tech
                columns.RelativeColumn(1); // Status
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Date").SemiBold();
                header.Cell().Element(CellStyle).Text("Patient").SemiBold();
                header.Cell().Element(CellStyle).Text("Lab Tech").SemiBold();
                header.Cell().Element(CellStyle).Text("Status").SemiBold();
            });

            foreach (var order in labOrders.Take(100))
            {
                table.Cell().Element(CellStyle).Text(order["OrderDate"] != null ? ((DateTime)order["OrderDate"]).ToString("yyyy-MM-dd") : "").FontSize(10);
                table.Cell().Element(CellStyle).Text(order["PatientName"]?.ToString() ?? "").FontSize(10);
                table.Cell().Element(CellStyle).Text(order["LabTechName"]?.ToString() ?? "").FontSize(10);
                table.Cell().Element(CellStyle).Text(order["Status"]?.ToString() ?? "").FontSize(10);
            }
        });

        if (labOrders.Count > 100)
        {
            col.Item().PaddingTop(0.5f, Unit.Centimetre).Text($"Note: Showing first 100 of {labOrders.Count} lab orders").FontSize(10).Italic();
        }
    }
}

        private static IContainer CellStyle(IContainer container)
        {
            return container.DefaultTextStyle(x => x.FontSize(9)).PaddingVertical(5).PaddingHorizontal(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        }

      public async Task MarkNotificationAsReadAsync(int notificationId)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            UPDATE Notifications 
            SET IsRead = 1 
            WHERE NotificationId = @NotificationId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@NotificationId", notificationId);

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Notification marked as read: {NotificationId}", notificationId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
        throw;
    }
}

public async Task MarkAllNotificationsAsReadAsync(int adminId)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            UPDATE Notifications 
            SET IsRead = 1 
            WHERE UserId = @AdminId AND IsRead = 0";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@AdminId", adminId);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Marked {Count} notifications as read for admin: {AdminId}", rowsAffected, adminId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error marking all notifications as read for admin: {AdminId}", adminId);
        throw;
    }
}

// Your existing GetNotificationsAsync method is already good
public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int adminId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var sql = @"
        SELECT NotificationId, UserId, Title, Message, IsRead, CreatedAt
        FROM dbo.Notifications
        WHERE UserId = @AdminId
        ORDER BY CreatedAt DESC";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@AdminId", adminId);

    var notifications = new List<NotificationDto>();
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        notifications.Add(new NotificationDto
        {
            NotificationId = reader.GetInt32(reader.GetOrdinal("NotificationId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
            Message = reader.IsDBNull(reader.GetOrdinal("Message")) ? null : reader.GetString(reader.GetOrdinal("Message")),
            IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        });
    }

    return notifications;
}

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}