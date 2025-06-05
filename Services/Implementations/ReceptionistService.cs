using BlazorApp1.Models.DTOs; 
using BlazorApp1.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;


namespace BlazorApp1.Services.Implementations
{
    public class ReceptionistService : IReceptionistService
    {
        private readonly string _connectionString;
  private readonly ILogger<ReceptionistService> _logger;
        public ReceptionistService(IConfiguration configuration, ILogger<ReceptionistService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
                 _logger = logger;
        }

        // Dashboard Statistics
        public async Task<int> GetTodayAppointmentsCountAsync()
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM dbo.Appointments 
                WHERE CAST(ScheduledDateTime AS DATE) = CAST(GETDATE() AS DATE)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<int> GetPendingAppointmentsCountAsync()
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM dbo.Appointments 
                WHERE Status = 'Pending'";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<int> GetTotalPatientsCountAsync()
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM dbo.PatientProfiles p
                INNER JOIN dbo.Users u ON p.PatientId = u.UserId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<List<AppointmentDto>> GetRecentAppointmentsAsync(int count = 10)
        {
            const string sql = @"
                SELECT TOP (@Count)
                    a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId,
                    a.ScheduledDateTime, a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
                    pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
                    du.FullName AS DoctorName,
                    ru.FullName AS ReceptionistName,
                    pp.Gender AS PatientGender,
                    DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge
                FROM dbo.Appointments a
                INNER JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
                INNER JOIN dbo.Users pu ON pp.PatientId = pu.UserId
                INNER JOIN dbo.DoctorProfiles dp ON a.DoctorId = dp.DoctorId
                INNER JOIN dbo.Users du ON dp.DoctorId = du.UserId
                LEFT JOIN dbo.ReceptionistProfiles rp ON a.ReceptionistId = rp.ReceptionistId
                LEFT JOIN dbo.Users ru ON rp.ReceptionistId = ru.UserId
                ORDER BY a.CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Count", count);

            var appointments = new List<AppointmentDto>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }

            return appointments;
        }

        public async Task<List<NotificationDto>> GetReceptionistNotificationsAsync(int receptionistId, int count = 5)
        {
            const string sql = @"
                SELECT TOP (@Count)
                    NotificationId, UserId, Title, Message, IsRead, CreatedAt
                FROM dbo.Notifications
                WHERE UserId = @ReceptionistId
                ORDER BY CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Count", count);
            command.Parameters.AddWithValue("@ReceptionistId", receptionistId);

            var notifications = new List<NotificationDto>();
            using var reader = await command.ExecuteReaderAsync();

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
        public async Task<bool> CheckInPatientAsync(int appointmentId, int receptionistId)
{
    const string sql = @"
        UPDATE dbo.Appointments
        SET Status = 'Completed', ReceptionistId = @ReceptionistId, UpdatedAt = SYSUTCDATETIME()
        WHERE AppointmentId = @AppointmentId
          AND Status NOT IN ('Cancelled','Completed')";

    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
    cmd.Parameters.AddWithValue("@ReceptionistId", receptionistId);

    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0;
}


        // Walk-in Registration
        public async Task<int> RegisterWalkInPatientAsync(PatientProfileDto patientProfile, string password)
        {
            const string insertUserSql = @"
        INSERT INTO dbo.Users (UserName, PasswordHash, CNIC, PhoneNumber, FullName, ProfilePictureUrl, CreatedAt, UpdatedAt)
        VALUES (@UserName, @PasswordHash, @CNIC, @PhoneNumber, @FullName, @ProfilePictureUrl, SYSUTCDATETIME(), SYSUTCDATETIME());
        SELECT CAST(SCOPE_IDENTITY() AS INT);";  // Return the newly inserted UserId

            const string insertUserRoleSql = @"
        INSERT INTO dbo.UserRoles (UserId, RoleId)
        VALUES (@UserId, (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Patient'));";

            const string insertPatientProfileSql = @"
        INSERT INTO dbo.PatientProfiles (PatientId, DateOfBirth, Gender, Address, BloodGroup)
        VALUES (@PatientId, @DateOfBirth, @Gender, @Address, @BloodGroup);";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1) Insert into dbo.Users and capture new UserId
                using var userCommand = new SqlCommand(insertUserSql, connection, transaction);
                userCommand.Parameters.AddWithValue("@UserName", patientProfile.UserName);
                userCommand.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                userCommand.Parameters.AddWithValue("@CNIC", patientProfile.CNIC);
                userCommand.Parameters.AddWithValue("@PhoneNumber", (object?)patientProfile.PhoneNumber ?? DBNull.Value);
                userCommand.Parameters.AddWithValue("@FullName", patientProfile.FullName);
                userCommand.Parameters.AddWithValue("@ProfilePictureUrl", (object?)patientProfile.ProfilePictureUrl ?? DBNull.Value);

                var newUserIdObj = await userCommand.ExecuteScalarAsync();
                int newUserId = Convert.ToInt32(newUserIdObj);

                // 2) Insert into dbo.UserRoles
                using var roleCommand = new SqlCommand(insertUserRoleSql, connection, transaction);
                roleCommand.Parameters.AddWithValue("@UserId", newUserId);
                await roleCommand.ExecuteNonQueryAsync();

                // 3) Insert into dbo.PatientProfiles
                using var patientCommand = new SqlCommand(insertPatientProfileSql, connection, transaction);
                patientCommand.Parameters.AddWithValue("@PatientId", newUserId);
                patientCommand.Parameters.AddWithValue("@DateOfBirth", (object?)patientProfile.DateOfBirth ?? DBNull.Value);
                patientCommand.Parameters.AddWithValue("@Gender", (object?)patientProfile.Gender ?? DBNull.Value);
                patientCommand.Parameters.AddWithValue("@Address", (object?)patientProfile.Address ?? DBNull.Value);
                patientCommand.Parameters.AddWithValue("@BloodGroup", (object?)patientProfile.BloodGroup ?? DBNull.Value);
                await patientCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return newUserId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
        // Doctor Information (for appointment scheduling)
        public async Task<List<DoctorProfileDto>> GetAllDoctorsAsync()
        {
            const string sql = @"
                SELECT 
                    dp.DoctorId, dp.Specialty, dp.Qualifications, dp.ConsultationFee,
                    u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
                FROM dbo.DoctorProfiles dp
                INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
                ORDER BY u.FullName";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);

            var doctors = new List<DoctorProfileDto>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                doctors.Add(MapDoctorFromReader(reader));
            }

            return doctors;
        }
public async Task<bool> IsDoctorSlotTakenAsync(int doctorId, DateTime scheduledDateTime)
{
    const string sql = @"
        SELECT COUNT(1)
        FROM dbo.Appointments
        WHERE DoctorId = @DoctorId
          AND ScheduledDateTime = @ScheduledDateTime
          AND Status NOT IN ('Cancelled', 'Completed')";

    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@DoctorId", doctorId);
    command.Parameters.AddWithValue("@ScheduledDateTime", scheduledDateTime);

    var count = (int)await command.ExecuteScalarAsync();
    return count > 0;
}
public async Task SendNotificationToDoctorAsync(int doctorId, string title, string message)
        {
            const string sql = @"
        INSERT INTO dbo.Notifications (UserId, Title, Message, IsRead, CreatedAt)
        VALUES (@DoctorId, @Title, @Message, 0, SYSUTCDATETIME())";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@DoctorId", doctorId);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Message", message);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DoctorProfileDto>> GetDoctorsBySpecialtyAsync(string specialty)
        {
            const string sql = @"
                SELECT 
                    dp.DoctorId, dp.Specialty, dp.LicenseNumber, dp.Experience, dp.Qualification, dp.ConsultationFee,
                    u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
                FROM dbo.DoctorProfiles dp
                INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
                WHERE dp.Specialty = @Specialty
                ORDER BY u.FullName";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Specialty", specialty);

            var doctors = new List<DoctorProfileDto>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                doctors.Add(MapDoctorFromReader(reader));
            }

            return doctors;
        }

        public async Task<DoctorProfileDto?> GetDoctorByIdAsync(int doctorId)
        {
            const string sql = @"
                SELECT 
                    dp.DoctorId, dp.Specialty, dp.LicenseNumber, dp.Experience, dp.Qualification,
                    u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
                FROM dbo.DoctorProfiles dp
                INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
                WHERE dp.DoctorId = @DoctorId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DoctorId", doctorId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapDoctorFromReader(reader);
            }

            return null;
        }

        // Profile Management
        public async Task<ReceptionistProfileDto?> GetReceptionistProfileAsync(int receptionistId)
        {
            const string sql = @"
                SELECT 
                    rp.ReceptionistId ,
                    u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
                FROM dbo.ReceptionistProfiles rp
                INNER JOIN dbo.Users u ON rp.ReceptionistId = u.UserId
                WHERE rp.ReceptionistId = @ReceptionistId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ReceptionistId", receptionistId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ReceptionistProfileDto
                {
                    ReceptionistId = reader.GetInt32(reader.GetOrdinal("ReceptionistId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
                };
            }

            return null;
        }

public async Task<bool> UpdateReceptionistProfileAsync(ReceptionistProfileDto profile)
{
    const string updateUserSql = @"
        UPDATE dbo.Users 
        SET 
            UserName = @UserName,
            CNIC = @CNIC,
            PhoneNumber = @PhoneNumber,
            FullName = @FullName,
            ProfilePictureUrl = @ProfilePictureUrl,
            UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @ReceptionistId";

    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();

    try
    {
        // Update Users table
        using var userCommand = new SqlCommand(updateUserSql, connection, transaction);
        userCommand.Parameters.AddWithValue("@ReceptionistId", profile.ReceptionistId);
        userCommand.Parameters.AddWithValue("@UserName", profile.UserName);
        userCommand.Parameters.AddWithValue("@CNIC", profile.CNIC);
        userCommand.Parameters.AddWithValue("@PhoneNumber", (object?)profile.PhoneNumber ?? DBNull.Value);
        userCommand.Parameters.AddWithValue("@FullName", profile.FullName);
        userCommand.Parameters.AddWithValue("@ProfilePictureUrl", (object?)profile.ProfilePictureUrl ?? DBNull.Value);
        await userCommand.ExecuteNonQueryAsync();

        // No ReceptionistProfiles update needed (no extra fields)
        await transaction.CommitAsync();
        return true;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

public async Task ChangePasswordAsync(EditReceptionistPassword dto)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // First, verify the current password
        var verifyQuery = "SELECT PasswordHash FROM Users WHERE UserId = @ReceptionistId";
        using var verifyCommand = new SqlCommand(verifyQuery, connection);
        verifyCommand.Parameters.AddWithValue("@ReceptionistId", dto.ReceptionistId);

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
            WHERE UserId = @ReceptionistId";

        using var updateCommand = new SqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@ReceptionistId", dto.ReceptionistId);
        updateCommand.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("Failed to update password.");
        }

        _logger.LogInformation("Password changed successfully for admin ID: {ReceptionistId}", dto.ReceptionistId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error changing password for admin ID: {ReceptionistId}", dto.ReceptionistId);
        throw;
    }
}

   

// You already have VerifyPassword(...) and HashPassword(...) elsewhere in this class.


        // Utility Methods
        public async Task<List<string>> GetAvailableStatusesAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "Pending",
                "Confirmed",
                "In Progress",
                "Completed",
                "Cancelled",
                "No Show"
            });
        }
        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        public async Task<List<string>> GetDoctorSpecialtiesAsync()
        {
            const string sql = "SELECT DISTINCT Specialty FROM dbo.DoctorProfiles WHERE Specialty IS NOT NULL ORDER BY Specialty";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);

            var specialties = new List<string>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                specialties.Add(reader.GetString(reader.GetOrdinal("Specialty")));
            }

            return specialties;
        }

        public async Task<bool> IsAppointmentTimeAvailableAsync(int doctorId, DateTime scheduledDateTime, int? excludeAppointmentId = null)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM dbo.Appointments 
                WHERE DoctorId = @DoctorId 
                  AND ScheduledDateTime = @ScheduledDateTime 
                  AND Status NOT IN ('Cancelled', 'Completed')";

            if (excludeAppointmentId.HasValue)
            {
                sql += " AND AppointmentId != @ExcludeAppointmentId";
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DoctorId", doctorId);
            command.Parameters.AddWithValue("@ScheduledDateTime", scheduledDateTime);

            if (excludeAppointmentId.HasValue)
            {
                command.Parameters.AddWithValue("@ExcludeAppointmentId", excludeAppointmentId.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count == 0;
        }

        // Helper Methods
        private static AppointmentDto MapAppointmentFromReader(SqlDataReader reader)
        {
            return new AppointmentDto
            {
                AppointmentId = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                ReceptionistId = reader.IsDBNull(reader.GetOrdinal("ReceptionistId")) ? null : reader.GetInt32(reader.GetOrdinal("ReceptionistId")),
                ScheduledDateTime = reader.GetDateTime(reader.GetOrdinal("ScheduledDateTime")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                PatientPhone = reader.IsDBNull(reader.GetOrdinal("PatientPhone")) ? null : reader.GetString(reader.GetOrdinal("PatientPhone")),
                DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                ReceptionistName = reader.IsDBNull(reader.GetOrdinal("ReceptionistName")) ? null : reader.GetString(reader.GetOrdinal("ReceptionistName")),
                PatientGender = reader.IsDBNull(reader.GetOrdinal("PatientGender")) ? null : reader.GetString(reader.GetOrdinal("PatientGender")),
                PatientAge = reader.IsDBNull(reader.GetOrdinal("PatientAge")) ? null : reader.GetInt32(reader.GetOrdinal("PatientAge")),
                ConsultationFee = reader.IsDBNull(reader.GetOrdinal("ConsultationFee")) ? null : reader.GetDecimal(reader.GetOrdinal("ConsultationFee"))
            };
        }

        private static PatientProfileDto MapPatientFromReader(SqlDataReader reader)
        {
            return new PatientProfileDto
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
            };
        }

        private static DoctorProfileDto MapDoctorFromReader(SqlDataReader reader)
        {
            return new DoctorProfileDto
            {
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                Specialty = reader.IsDBNull(reader.GetOrdinal("Specialty")) ? null : reader.GetString(reader.GetOrdinal("Specialty")),
                Qualifications = reader.IsDBNull(reader.GetOrdinal("Qualifications")) ? null : reader.GetString(reader.GetOrdinal("Qualifications")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                CNIC = reader.GetString(reader.GetOrdinal("CNIC")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl")),
                ConsultationFee = reader.IsDBNull(reader.GetOrdinal("ConsultationFee")) ? null : reader.GetDecimal(reader.GetOrdinal("ConsultationFee"))
            };
        }
        // Missing methods for ReceptionistService.cs

        public async Task<bool> CheckCNICExistsAsync(string cnic)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE CNIC = @CNIC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CNIC", cnic);

            await connection.OpenAsync();
            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<bool> CheckUserNameExistsAsync(string userName)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE UserName = @UserName";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserName", userName);

            await connection.OpenAsync();
            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<List<AppointmentDto>> GetAllAppointmentsAsync(int pageNumber = 1, int pageSize = 20)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        ORDER BY a.ScheduledDateTime DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var appointments = new List<AppointmentDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }
            return appointments;
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByDateAsync(DateTime date)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        WHERE CAST(a.ScheduledDateTime AS DATE) = CAST(@Date AS DATE)
        ORDER BY a.ScheduledDateTime";

            var appointments = new List<AppointmentDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Date", date);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }
            return appointments;
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByStatusAsync(string status)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        WHERE a.Status = @Status
        ORDER BY a.ScheduledDateTime DESC";

            var appointments = new List<AppointmentDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", status);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }
            return appointments;
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByPatientAsync(int patientId)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        WHERE a.PatientId = @PatientId
        ORDER BY a.ScheduledDateTime DESC";

            var appointments = new List<AppointmentDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }
            return appointments;
        }

        public async Task<List<AppointmentDto>> GetAppointmentsByDoctorAsync(int doctorId)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        WHERE a.DoctorId = @DoctorId
        ORDER BY a.ScheduledDateTime DESC";

            var appointments = new List<AppointmentDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DoctorId", doctorId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }
            return appointments;
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        WHERE a.AppointmentId = @AppointmentId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AppointmentId", appointmentId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapAppointmentFromReader(reader);
            }
            return null;
        }

        public async Task<bool> UpdateAppointmentStatusAsync(int appointmentId, string status, int receptionistId)
        {
            const string sql = @"
        UPDATE dbo.Appointments 
        SET Status = @Status, ReceptionistId = @ReceptionistId, UpdatedAt = SYSUTCDATETIME() 
        WHERE AppointmentId = @AppointmentId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AppointmentId", appointmentId);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@ReceptionistId", receptionistId);

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> RescheduleAppointmentAsync(int appointmentId, DateTime newDateTime, int receptionistId)
        {
            const string sql = @"
        UPDATE dbo.Appointments 
        SET ScheduledDateTime = @ScheduledDateTime, ReceptionistId = @ReceptionistId, UpdatedAt = SYSUTCDATETIME() 
        WHERE AppointmentId = @AppointmentId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AppointmentId", appointmentId);
            command.Parameters.AddWithValue("@ScheduledDateTime", newDateTime);
            command.Parameters.AddWithValue("@ReceptionistId", receptionistId);

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> AssignReceptionistToAppointmentAsync(int appointmentId, int receptionistId)
        {
            const string sql = @"
        UPDATE dbo.Appointments 
        SET ReceptionistId = @ReceptionistId, UpdatedAt = SYSUTCDATETIME() 
        WHERE AppointmentId = @AppointmentId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AppointmentId", appointmentId);
            command.Parameters.AddWithValue("@ReceptionistId", receptionistId);

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

      public async Task<int> CreateAppointmentAsync(AppointmentDto appointment)
{
    const string sql = @"
        INSERT INTO dbo.Appointments 
            (PatientId, DoctorId, ReceptionistId, ScheduledDateTime, Status, ConsultationFee, CreatedAt, UpdatedAt)
        VALUES 
            (@PatientId, @DoctorId, @ReceptionistId, @ScheduledDateTime, @Status, @ConsultationFee, SYSUTCDATETIME(), SYSUTCDATETIME());
        SELECT SCOPE_IDENTITY();";

    using var connection = new SqlConnection(_connectionString);
    using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@PatientId", appointment.PatientId);
    command.Parameters.AddWithValue("@DoctorId", appointment.DoctorId);
    command.Parameters.AddWithValue("@ReceptionistId", (object?)appointment.ReceptionistId ?? DBNull.Value);
    command.Parameters.AddWithValue("@ScheduledDateTime", appointment.ScheduledDateTime);
    command.Parameters.AddWithValue("@Status", appointment.Status);
    command.Parameters.AddWithValue("@ConsultationFee", (object?)appointment.ConsultationFee ?? DBNull.Value);

    await connection.OpenAsync();
    var newIdObj = await command.ExecuteScalarAsync();
    return Convert.ToInt32(newIdObj);
}
        public async Task<bool> CancelAppointmentAsync(int appointmentId, int receptionistId)
        {
            const string sql = @"
        UPDATE dbo.Appointments 
        SET Status = 'Cancelled', ReceptionistId = @ReceptionistId, UpdatedAt = SYSUTCDATETIME() 
        WHERE AppointmentId = @AppointmentId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AppointmentId", appointmentId);
            command.Parameters.AddWithValue("@ReceptionistId", receptionistId);

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<List<PatientProfileDto>> GetAllPatientsAsync(int pageNumber = 1, int pageSize = 20)
        {
            const string sql = @"
        SELECT p.PatientId, p.DateOfBirth, p.Gender, p.Address, p.BloodGroup,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.PatientProfiles p
        INNER JOIN dbo.Users u ON p.PatientId = u.UserId
        ORDER BY u.FullName
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var patients = new List<PatientProfileDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                patients.Add(MapPatientFromReader(reader));
            }
            return patients;
        }

        public async Task<List<PatientProfileDto>> SearchPatientsAsync(string searchTerm)
        {
            const string sql = @"
        SELECT p.PatientId, p.DateOfBirth, p.Gender, p.Address, p.BloodGroup,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.PatientProfiles p
        INNER JOIN dbo.Users u ON p.PatientId = u.UserId
        WHERE u.FullName LIKE '%' + @SearchTerm + '%' OR u.CNIC LIKE '%' + @SearchTerm + '%'
        ORDER BY u.FullName";

            var patients = new List<PatientProfileDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SearchTerm", searchTerm);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                patients.Add(MapPatientFromReader(reader));
            }
            return patients;
        }

        public async Task<PatientProfileDto?> GetPatientByIdAsync(int patientId)
        {
            const string sql = @"
        SELECT p.PatientId, p.DateOfBirth, p.Gender, p.Address, p.BloodGroup,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.PatientProfiles p
        INNER JOIN dbo.Users u ON p.PatientId = u.UserId
        WHERE p.PatientId = @PatientId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapPatientFromReader(reader);
            }
            return null;
        }

        public async Task<PatientProfileDto?> GetPatientByCNICAsync(string cnic)
        {
            const string sql = @"
        SELECT p.PatientId, p.DateOfBirth, p.Gender, p.Address, p.BloodGroup,
               u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
        FROM dbo.PatientProfiles p
        INNER JOIN dbo.Users u ON p.PatientId = u.UserId
        WHERE u.CNIC = @CNIC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CNIC", cnic);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapPatientFromReader(reader);
            }
            return null;
        }

        public async Task<List<AppointmentDto>> GetPatientAppointmentHistoryAsync(int patientId)
        {
            const string sql = @"
        SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId, a.ScheduledDateTime, 
               a.Status, a.CreatedAt, a.UpdatedAt,a.ConsultationFee,
               pu.FullName AS PatientName, pu.PhoneNumber AS PatientPhone,
               pp.Gender AS PatientGender,
               DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) AS PatientAge,
               du.FullName AS DoctorName,
               ru.FullName AS ReceptionistName
        FROM dbo.Appointments a
        INNER JOIN dbo.Users pu ON a.PatientId = pu.UserId
        INNER JOIN dbo.Users du ON a.DoctorId = du.UserId
        LEFT JOIN dbo.Users ru ON a.ReceptionistId = ru.UserId
        LEFT JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
        WHERE a.PatientId = @PatientId
        ORDER BY a.ScheduledDateTime DESC";

            var appointments = new List<AppointmentDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(MapAppointmentFromReader(reader));
            }
            return appointments;
        }

        public async Task<bool> UpdatePatientProfileAsync(PatientProfileDto patientProfile)
        {
            const string updateUserSql = @"
        UPDATE dbo.Users 
        SET FullName = @FullName, PhoneNumber = @PhoneNumber, UpdatedAt = SYSUTCDATETIME() 
        WHERE UserId = @PatientId";

            const string updatePatientSql = @"
        UPDATE dbo.PatientProfiles 
        SET DateOfBirth = @DateOfBirth, Gender = @Gender, Address = @Address, BloodGroup = @BloodGroup 
        WHERE PatientId = @PatientId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update Users table
                using var userCommand = new SqlCommand(updateUserSql, connection, transaction);
                userCommand.Parameters.AddWithValue("@PatientId", patientProfile.PatientId);
                userCommand.Parameters.AddWithValue("@FullName", patientProfile.FullName);
                userCommand.Parameters.AddWithValue("@PhoneNumber", (object?)patientProfile.PhoneNumber ?? DBNull.Value);
                await userCommand.ExecuteNonQueryAsync();

                // Update PatientProfiles table
                using var patientCommand = new SqlCommand(updatePatientSql, connection, transaction);
                patientCommand.Parameters.AddWithValue("@PatientId", patientProfile.PatientId);
                patientCommand.Parameters.AddWithValue("@DateOfBirth", (object?)patientProfile.DateOfBirth ?? DBNull.Value);
                patientCommand.Parameters.AddWithValue("@Gender", (object?)patientProfile.Gender ?? DBNull.Value);
                patientCommand.Parameters.AddWithValue("@Address", (object?)patientProfile.Address ?? DBNull.Value);
                patientCommand.Parameters.AddWithValue("@BloodGroup", (object?)patientProfile.BloodGroup ?? DBNull.Value);
                await patientCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}