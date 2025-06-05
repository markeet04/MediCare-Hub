// File: Services/PatientService.cs
using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp1.Services
{
    public class PatientService : IPatientService
    {
        private readonly string _connectionString;
        private readonly ILogger<PatientService>? _logger;

        public PatientService(IConfiguration configuration, ILogger<PatientService>? logger = null)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection");
            _logger = logger;
        }
// ...existing code...

public async Task<bool> UpdatePatientProfileAsync(int patientId, UpdatePatientProfileRequest request)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Update Users table (FullName, PhoneNumber, ProfilePictureUrl)
        var userSql = @"
            UPDATE Users
            SET FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                ProfilePictureUrl = @ProfilePictureUrl
            WHERE UserId = @PatientId";

        using (var userCmd = new SqlCommand(userSql, conn))
        {
            userCmd.Parameters.AddWithValue("@FullName", (object?)request.FullName ?? DBNull.Value);
            userCmd.Parameters.AddWithValue("@PhoneNumber", (object?)request.PhoneNumber ?? DBNull.Value);
            userCmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)request.ProfilePictureUrl ?? DBNull.Value);
            userCmd.Parameters.AddWithValue("@PatientId", patientId);
            await userCmd.ExecuteNonQueryAsync();
        }

        // Update PatientProfiles table (DateOfBirth, Gender, BloodGroup, Address)
        var profileSql = @"
            UPDATE PatientProfiles
            SET 
                DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                BloodGroup = @BloodGroup,
                Address = @Address
            WHERE PatientId = @PatientId";

        using (var profileCmd = new SqlCommand(profileSql, conn))
        {
            profileCmd.Parameters.AddWithValue("@DateOfBirth", (object?)request.DateOfBirth ?? DBNull.Value);
            profileCmd.Parameters.AddWithValue("@Gender", (object?)request.Gender ?? DBNull.Value);
            profileCmd.Parameters.AddWithValue("@BloodGroup", (object?)request.BloodGroup ?? DBNull.Value);
            profileCmd.Parameters.AddWithValue("@Address", (object?)request.Address ?? DBNull.Value);
            profileCmd.Parameters.AddWithValue("@PatientId", patientId);
            await profileCmd.ExecuteNonQueryAsync();
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error updating patient profile for PatientId: {PatientId}", patientId);
        throw new Exception("Failed to update patient profile.");
    }
}

public async Task<bool> ChangePasswordAsync(ChangePatientPasswordRequest request)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Get current password hash
        var getHashSql = "SELECT PasswordHash FROM Users WHERE UserId = @PatientId";
        string? currentHash = null;
        using (var getHashCmd = new SqlCommand(getHashSql, conn))
        {
            getHashCmd.Parameters.AddWithValue("@PatientId", request.PatientId);
            var result = await getHashCmd.ExecuteScalarAsync();
            currentHash = result as string;
        }

        if (currentHash == null)
            throw new Exception("User not found.");

        // Verify current password
        if (HashPassword(request.CurrentPassword) != currentHash)
            throw new Exception("Current password is incorrect.");

        // Update password
        var newHash = HashPassword(request.NewPassword);
        var updateSql = "UPDATE Users SET PasswordHash = @NewHash, UpdatedAt = SYSUTCDATETIME() WHERE UserId = @PatientId";
        using (var updateCmd = new SqlCommand(updateSql, conn))
        {
            updateCmd.Parameters.AddWithValue("@NewHash", newHash);
            updateCmd.Parameters.AddWithValue("@PatientId", request.PatientId);
            await updateCmd.ExecuteNonQueryAsync();
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error changing password for PatientId: {PatientId}", request.PatientId);
        throw new Exception("Failed to change password: " + ex.Message);
    }
}

// ...existing code...

// Helper method for hashing passwords (if not already present)

        public async Task<(int userId, string tempPassword)> CreatePatientAsync(CreatePatientDto dto, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                var passwordHash = HashPassword(password);

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
                return (userId, password);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        
    
            /// <summary>
        /// Gets the most recent notifications for a user.
        /// </summary>
        public async Task<List<NotificationDto>> GetRecentNotificationsAsync(int userId)
        {
            var notifications = new List<NotificationDto>();
            const string query = @"
                    SELECT TOP 10 NotificationId, UserId, Title, Message, IsRead, CreatedAt
                    FROM Notifications
                    WHERE UserId = @UserId
                    ORDER BY CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notifications.Add(new NotificationDto
                {
                    NotificationId = reader.GetInt32(reader.GetOrdinal("NotificationId")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Message = reader.GetString(reader.GetOrdinal("Message")),
                    IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
            return notifications;
        }
    
            /// <summary>
            /// Gets the count of unread notifications for a user.
            /// </summary>
            public async Task<int> GetUnreadCountAsync(int userId)
            {
                const string query = @"
                    SELECT COUNT(*) FROM Notifications
                    WHERE UserId = @UserId AND IsRead = 0";
    
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
    
                await connection.OpenAsync();
                var count = await command.ExecuteScalarAsync();
                return Convert.ToInt32(count);
            }
    
            /// <summary>
            /// Marks all notifications as read for a user.
            /// </summary>
            public async Task MarkAllNotificationsReadAsync(int userId)
            {
                const string query = @"
                    UPDATE Notifications
                    SET IsRead = 1
                    WHERE UserId = @UserId AND IsRead = 0";
    
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
    
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        

        public async Task<PatientProfileDto?> GetPatientByUserIdAsync(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    pp.PatientId,
                    pp.DateOfBirth,
                    pp.Gender,
                    pp.Address,
                    pp.BloodGroup,
                    u.UserName,
                    u.CNIC,
                    u.PhoneNumber,
                    u.FullName,
                    u.ProfilePictureUrl
                FROM dbo.PatientProfiles pp
                INNER JOIN dbo.Users u ON pp.PatientId = u.UserId
                WHERE u.UserId = @UserId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
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
            return null;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    
     public async Task<PatientHistoryDto?> GetLatestHistoryAsync(int patientId)
        {
            const string query = @"
                SELECT TOP 1 
                    HistoryId, PatientId, DoctorId, EncounterDate, Symptoms, Notes,
                    WeightKg, HeightCm, BMI, PulseBPM, BloodPressure, TemperatureC,
                    RespiratoryRate, OxygenSatPct, Ethnicity, SmokingStatus,
                    TotalCholesterol, HDLCholesterol, DiabetesType, FamilyCVDHistory,
                    OnBPMedication, OnStatin, WaistCm, PhysicalActivity,
                    EatsVegetablesDaily, HighBloodGlucoseHx, FamilyDiabetesHistory,
                    Confusion, BloodUreaMmolPerL
                FROM PatientHistory 
                WHERE PatientId = @PatientId 
                ORDER BY EncounterDate DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

         if (await reader.ReadAsync())
{
    return new PatientHistoryDto
    {
        HistoryId = reader.GetInt32(reader.GetOrdinal("HistoryId")),
        PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
        DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
        EncounterDate = reader.GetDateTime(reader.GetOrdinal("EncounterDate")),
        Symptoms = reader.IsDBNull(reader.GetOrdinal("Symptoms")) ? null : reader.GetString(reader.GetOrdinal("Symptoms")),
        Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
        WeightKg = reader.IsDBNull(reader.GetOrdinal("WeightKg")) ? null : reader.GetDecimal(reader.GetOrdinal("WeightKg")),
        HeightCm = reader.IsDBNull(reader.GetOrdinal("HeightCm")) ? null : reader.GetDecimal(reader.GetOrdinal("HeightCm")),
        BMI = reader.IsDBNull(reader.GetOrdinal("BMI")) ? null : reader.GetDecimal(reader.GetOrdinal("BMI")),
        PulseBPM = reader.IsDBNull(reader.GetOrdinal("PulseBPM")) ? null : reader.GetInt32(reader.GetOrdinal("PulseBPM")),
        BloodPressure = reader.IsDBNull(reader.GetOrdinal("BloodPressure")) ? null : reader.GetString(reader.GetOrdinal("BloodPressure")),
        TemperatureC = reader.IsDBNull(reader.GetOrdinal("TemperatureC")) ? null : reader.GetDecimal(reader.GetOrdinal("TemperatureC")),
        RespiratoryRate = reader.IsDBNull(reader.GetOrdinal("RespiratoryRate")) ? null : reader.GetInt32(reader.GetOrdinal("RespiratoryRate")),
        OxygenSatPct = reader.IsDBNull(reader.GetOrdinal("OxygenSatPct")) ? null : reader.GetDecimal(reader.GetOrdinal("OxygenSatPct")),
        Ethnicity = reader.IsDBNull(reader.GetOrdinal("Ethnicity")) ? null : reader.GetString(reader.GetOrdinal("Ethnicity")),
        SmokingStatus = reader.IsDBNull(reader.GetOrdinal("SmokingStatus")) ? null : reader.GetString(reader.GetOrdinal("SmokingStatus")),
        TotalCholesterol = reader.IsDBNull(reader.GetOrdinal("TotalCholesterol")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalCholesterol")),
        HDLCholesterol = reader.IsDBNull(reader.GetOrdinal("HDLCholesterol")) ? null : reader.GetDecimal(reader.GetOrdinal("HDLCholesterol")),
        DiabetesType = reader.IsDBNull(reader.GetOrdinal("DiabetesType")) ? null : reader.GetString(reader.GetOrdinal("DiabetesType")),
        FamilyCVDHistory = reader.IsDBNull(reader.GetOrdinal("FamilyCVDHistory")) ? null : reader.GetBoolean(reader.GetOrdinal("FamilyCVDHistory")),
        OnBPMedication = reader.IsDBNull(reader.GetOrdinal("OnBPMedication")) ? null : reader.GetBoolean(reader.GetOrdinal("OnBPMedication")),
        OnStatin = reader.IsDBNull(reader.GetOrdinal("OnStatin")) ? null : reader.GetBoolean(reader.GetOrdinal("OnStatin")),
        WaistCm = reader.IsDBNull(reader.GetOrdinal("WaistCm")) ? null : reader.GetDecimal(reader.GetOrdinal("WaistCm")),
        PhysicalActivity = reader.IsDBNull(reader.GetOrdinal("PhysicalActivity")) ? null : reader.GetBoolean(reader.GetOrdinal("PhysicalActivity")),
        EatsVegetablesDaily = reader.IsDBNull(reader.GetOrdinal("EatsVegetablesDaily")) ? null : reader.GetBoolean(reader.GetOrdinal("EatsVegetablesDaily")),
        HighBloodGlucoseHx = reader.IsDBNull(reader.GetOrdinal("HighBloodGlucoseHx")) ? null : reader.GetBoolean(reader.GetOrdinal("HighBloodGlucoseHx")),
        FamilyDiabetesHistory = reader.IsDBNull(reader.GetOrdinal("FamilyDiabetesHistory")) ? null : reader.GetBoolean(reader.GetOrdinal("FamilyDiabetesHistory")),
        Confusion = reader.IsDBNull(reader.GetOrdinal("Confusion")) ? null : reader.GetBoolean(reader.GetOrdinal("Confusion")),
        BloodUreaMmolPerL = reader.IsDBNull(reader.GetOrdinal("BloodUreaMmolPerL")) ? null : reader.GetDecimal(reader.GetOrdinal("BloodUreaMmolPerL"))
    };
}

            return null;
        }

        /// <summary>
        /// Fetches all future appointments for the given patient
        /// </summary>
        public async Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync(int patientId)
        {
            const string query = @"
                SELECT 
                    a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId,
                    a.ScheduledDateTime, a.Status, a.CreatedAt, a.UpdatedAt,
                    p.FullName as PatientName, p.PhoneNumber as PatientPhone,
                    DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) as PatientAge,
                    pp.Gender as PatientGender,
                    d.FullName as DoctorName,
                    r.FullName as ReceptionistName,
                    dp.ConsultationFee
                FROM Appointments a
                INNER JOIN Users p ON a.PatientId = p.UserId
                INNER JOIN Users d ON a.DoctorId = d.UserId
                LEFT JOIN Users r ON a.ReceptionistId = r.UserId
                LEFT JOIN PatientProfiles pp ON a.PatientId = pp.PatientId
                LEFT JOIN DoctorProfiles dp ON a.DoctorId = dp.DoctorId
                WHERE a.PatientId = @PatientId 
                AND a.ScheduledDateTime >= CAST(GETDATE() AS DATE)
                ORDER BY a.ScheduledDateTime ASC";

            var appointments = new List<AppointmentDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

         while (await reader.ReadAsync())
{
    appointments.Add(new AppointmentDto
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
        PatientAge = reader.IsDBNull(reader.GetOrdinal("PatientAge")) ? null : reader.GetInt32(reader.GetOrdinal("PatientAge")),
        PatientGender = reader.IsDBNull(reader.GetOrdinal("PatientGender")) ? null : reader.GetString(reader.GetOrdinal("PatientGender")),
        DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
        ReceptionistName = reader.IsDBNull(reader.GetOrdinal("ReceptionistName")) ? null : reader.GetString(reader.GetOrdinal("ReceptionistName")),
        ConsultationFee = reader.IsDBNull(reader.GetOrdinal("ConsultationFee")) ? null : reader.GetDecimal(reader.GetOrdinal("ConsultationFee"))
    });
}

            return appointments;
        }

        /// <summary>
        /// Fetches all appointments for the given patient
        /// </summary>
        public async Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId)
        {
            const string query = @"
                SELECT 
                    a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId,
                    a.ScheduledDateTime, a.Status, a.CreatedAt, a.UpdatedAt,
                    p.FullName as PatientName, p.PhoneNumber as PatientPhone,
                    DATEDIFF(YEAR, ISNULL(pp.DateOfBirth, '1990-01-01'), GETDATE()) as PatientAge,
                    ISNULL(pp.Gender, 'Not Specified') as PatientGender,
                    d.FullName as DoctorName,
                    ISNULL(r.FullName, 'Not Assigned') as ReceptionistName,
                    ISNULL(dp.ConsultationFee, 0) as ConsultationFee,
                    'General Medicine' as Department
                FROM Appointments a
                INNER JOIN Users p ON a.PatientId = p.UserId
                INNER JOIN Users d ON a.DoctorId = d.UserId
                LEFT JOIN Users r ON a.ReceptionistId = r.UserId
                LEFT JOIN PatientProfiles pp ON a.PatientId = pp.PatientId
                LEFT JOIN DoctorProfiles dp ON a.DoctorId = dp.DoctorId
                WHERE a.PatientId = @PatientId 
                ORDER BY a.ScheduledDateTime DESC";

            var appointments = new List<AppointmentDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        appointments.Add(new AppointmentDto
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
            PatientAge = reader.GetInt32(reader.GetOrdinal("PatientAge")),
            PatientGender = reader.GetString(reader.GetOrdinal("PatientGender")),
            DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
            ReceptionistName = reader.GetString(reader.GetOrdinal("ReceptionistName")),
            ConsultationFee = reader.GetDecimal(reader.GetOrdinal("ConsultationFee")),
            Department = reader.GetString(reader.GetOrdinal("Department"))
        });
    }

            return appointments;
        }

        

        /// <summary>
        /// Cancels an appointment and sends notifications
        /// </summary>
        public async Task<bool> CancelAppointmentAsync(int appointmentId, int patientId, string reason = "Cancelled by patient")
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get appointment details first
                var appointment = await GetAppointmentByIdAsync(appointmentId);
                if (appointment == null || appointment.PatientId != patientId)
                    return false;

                // Update appointment status
                const string updateQuery = @"
                    UPDATE Appointments 
                    SET Status = 'Cancelled', UpdatedAt = SYSUTCDATETIME()
                    WHERE AppointmentId = @AppointmentId AND PatientId = @PatientId";

                using var updateCmd = new SqlCommand(updateQuery, connection, transaction);
                updateCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                updateCmd.Parameters.AddWithValue("@PatientId", patientId);

                var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Send notification to doctor
                await SendNotificationInternalAsync(appointment.DoctorId,
                    "Appointment Cancelled",
                    $"Patient {appointment.PatientName} has cancelled their appointment scheduled for {appointment.ScheduledDateTime:MMM dd, yyyy at hh:mm tt}. Reason: {reason}",
                    connection, transaction);

                // Send notification to receptionist if assigned
                if (appointment.ReceptionistId.HasValue)
                {
                    await SendNotificationInternalAsync(appointment.ReceptionistId.Value,
                        "Appointment Cancelled",
                        $"Patient {appointment.PatientName} has cancelled their appointment with Dr. {appointment.DoctorName} scheduled for {appointment.ScheduledDateTime:MMM dd, yyyy at hh:mm tt}. Reason: {reason}",
                        connection, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Sends a notification to a user
        /// </summary>
        public async Task<bool> SendNotificationAsync(int userId, string title, string message)
        {
            using var connection = new SqlConnection(_connectionString);
            return await SendNotificationInternalAsync(userId, title, message, connection, null);
        }

        /// <summary>
        /// Internal method to send notifications within a transaction
        /// </summary>
        private async Task<bool> SendNotificationInternalAsync(int userId, string title, string message, SqlConnection connection, SqlTransaction? transaction)
        {
            const string query = @"
                INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt)
                VALUES (@UserId, @Title, @Message, 0, SYSUTCDATETIME())";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@Message", message);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Gets appointment by ID
        /// </summary>
        public async Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId)
        {
            const string query = @"
                SELECT 
                    a.AppointmentId, a.PatientId, a.DoctorId, a.ReceptionistId,
                    a.ScheduledDateTime, a.Status, a.CreatedAt, a.UpdatedAt,
                    p.FullName as PatientName, p.PhoneNumber as PatientPhone,
                    DATEDIFF(YEAR, ISNULL(pp.DateOfBirth, '1990-01-01'), GETDATE()) as PatientAge,
                    ISNULL(pp.Gender, 'Not Specified') as PatientGender,
                    d.FullName as DoctorName,
                    ISNULL(r.FullName, 'Not Assigned') as ReceptionistName,
                    ISNULL(dp.ConsultationFee, 0) as ConsultationFee,
                    'General Medicine' as Department
                FROM Appointments a
                INNER JOIN Users p ON a.PatientId = p.UserId
                INNER JOIN Users d ON a.DoctorId = d.UserId
                LEFT JOIN Users r ON a.ReceptionistId = r.UserId
                LEFT JOIN PatientProfiles pp ON a.PatientId = pp.PatientId
                LEFT JOIN DoctorProfiles dp ON a.DoctorId = dp.DoctorId
                WHERE a.AppointmentId = @AppointmentId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@AppointmentId", appointmentId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
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
                    PatientAge = reader.GetInt32(reader.GetOrdinal("PatientAge")),
                    PatientGender = reader.GetString(reader.GetOrdinal("PatientGender")),
                    DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                    ReceptionistName = reader.GetString(reader.GetOrdinal("ReceptionistName")),
                    ConsultationFee = reader.GetDecimal(reader.GetOrdinal("ConsultationFee")),
                    Department = reader.GetString(reader.GetOrdinal("Department"))
                };
            }

            return null;
        }

        /// <summary>
        /// Books a new appointment for a patient
        /// </summary>
        public async Task<bool> BookAppointmentAsync(CreateAppointmentDto appointmentDto)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert the appointment
                const string appointmentQuery = @"
                    INSERT INTO Appointments (PatientId, DoctorId, ScheduledDateTime, Status, CreatedAt, UpdatedAt)
                    VALUES (@PatientId, @DoctorId, @ScheduledDateTime, 'Pending', SYSUTCDATETIME(), SYSUTCDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var appointmentCmd = new SqlCommand(appointmentQuery, connection, transaction);
                appointmentCmd.Parameters.AddWithValue("@PatientId", appointmentDto.PatientId);
                appointmentCmd.Parameters.AddWithValue("@DoctorId", appointmentDto.DoctorId);
                appointmentCmd.Parameters.AddWithValue("@ScheduledDateTime", appointmentDto.ScheduledDateTime);

                var appointmentId = Convert.ToInt32(await appointmentCmd.ExecuteScalarAsync());

                // Get patient and doctor names for notifications
                const string namesQuery = @"
                    SELECT p.FullName as PatientName, d.FullName as DoctorName
                    FROM Users p, Users d
                    WHERE p.UserId = @PatientId AND d.UserId = @DoctorId";

                using var namesCmd = new SqlCommand(namesQuery, connection, transaction);
                namesCmd.Parameters.AddWithValue("@PatientId", appointmentDto.PatientId);
                namesCmd.Parameters.AddWithValue("@DoctorId", appointmentDto.DoctorId);

                using var reader = await namesCmd.ExecuteReaderAsync();
                string patientName = "";
                string doctorName = "";
                
                if (await reader.ReadAsync())
                {
                    patientName = reader.GetString(reader.GetOrdinal("PatientName"));
                    doctorName = reader.GetString(reader.GetOrdinal("DoctorName"));
                }
                await reader.CloseAsync();

                // Send notification to doctor
                await SendNotificationInternalAsync(appointmentDto.DoctorId, 
                    "New Appointment Request", 
                    $"Patient {patientName} has requested an appointment on {appointmentDto.ScheduledDateTime:MMM dd, yyyy at hh:mm tt}. Reason: {appointmentDto.ReasonForVisit}",
                    connection, transaction);

                // Send notification to patient
                await SendNotificationInternalAsync(appointmentDto.PatientId, 
                    "Appointment Booking Confirmation", 
                    $"Your appointment request with Dr. {doctorName} for {appointmentDto.ScheduledDateTime:MMM dd, yyyy at hh:mm tt} has been submitted and is pending approval.",
                    connection, transaction);

                // Send notifications to all receptionists
                await SendNotificationsToReceptionistsAsync(patientName, doctorName, appointmentDto.ScheduledDateTime, connection, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error booking appointment: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends notifications to all receptionists about a new appointment booking
        /// </summary>
        private async Task SendNotificationsToReceptionistsAsync(string patientName, string doctorName, DateTime scheduledDateTime, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Get all receptionist IDs
                const string receptionistQuery = @"
                    SELECT r.ReceptionistId
                    FROM ReceptionistProfiles r
                    INNER JOIN Users u ON r.ReceptionistId = u.UserId";

                using var receptionistCmd = new SqlCommand(receptionistQuery, connection, transaction);
                using var receptionistReader = await receptionistCmd.ExecuteReaderAsync();

                var receptionistIds = new List<int>();
                while (await receptionistReader.ReadAsync())
                {
                    receptionistIds.Add(receptionistReader.GetInt32(receptionistReader.GetOrdinal("ReceptionistId")));
                }
                await receptionistReader.CloseAsync();

                // Send notification to each receptionist
                string title = "New Patient Appointment Request";
                string message = $"Patient {patientName} has booked an appointment with Dr. {doctorName} for {scheduledDateTime:MMM dd, yyyy at hh:mm tt}. Please review and confirm the appointment.";

                foreach (var receptionistId in receptionistIds)
                {
                    await SendNotificationInternalAsync(receptionistId, title, message, connection, transaction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notifications to receptionists: {ex.Message}");
                // Don't throw here - we don't want to fail the entire appointment booking if notifications fail
            }
        }
public async Task<List<PatientHistoryDto>> GetAllPatientHistoriesAsync(int patientId)
{
    try
    {
        var query = @"
        SELECT 
            h.HistoryId,
            h.PatientId,
            h.DoctorId,
            h.EncounterDate,
            h.Symptoms,
            h.Notes,
            h.WeightKg,
            h.HeightCm,
            h.BMI,
            h.PulseBPM,
            h.BloodPressure,
            h.TemperatureC,
            h.RespiratoryRate,
            h.OxygenSatPct,
            h.Ethnicity,
            h.SmokingStatus,
            h.TotalCholesterol,
            h.HDLCholesterol,
            h.DiabetesType,
            h.FamilyCVDHistory,
            h.OnBPMedication,
            h.OnStatin,
            h.WaistCm,
            h.PhysicalActivity,
            h.EatsVegetablesDaily,
            h.HighBloodGlucoseHx,
            h.FamilyDiabetesHistory,
            h.Confusion,
            h.BloodUreaMmolPerL,
            d.FullName as DoctorName,
            dp.Specialty as DoctorSpecialty
        FROM PatientHistory h
        LEFT JOIN Users d ON h.DoctorId = d.UserId
        LEFT JOIN DoctorProfiles dp ON h.DoctorId = dp.DoctorId
        WHERE h.PatientId = @PatientId
        ORDER BY h.EncounterDate DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var histories = new List<PatientHistoryDto>();
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PatientId", patientId);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            histories.Add(new PatientHistoryDto
            {
                HistoryId = reader.GetInt32(reader.GetOrdinal("HistoryId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                EncounterDate = reader.GetDateTime(reader.GetOrdinal("EncounterDate")),
                Symptoms = reader.IsDBNull(reader.GetOrdinal("Symptoms")) ? null : reader.GetString(reader.GetOrdinal("Symptoms")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
                WeightKg = reader.IsDBNull(reader.GetOrdinal("WeightKg")) ? null : reader.GetDecimal(reader.GetOrdinal("WeightKg")),
                HeightCm = reader.IsDBNull(reader.GetOrdinal("HeightCm")) ? null : reader.GetDecimal(reader.GetOrdinal("HeightCm")),
                BMI = reader.IsDBNull(reader.GetOrdinal("BMI")) ? null : reader.GetDecimal(reader.GetOrdinal("BMI")),
                PulseBPM = reader.IsDBNull(reader.GetOrdinal("PulseBPM")) ? null : reader.GetInt32(reader.GetOrdinal("PulseBPM")),
                BloodPressure = reader.IsDBNull(reader.GetOrdinal("BloodPressure")) ? null : reader.GetString(reader.GetOrdinal("BloodPressure")),
                TemperatureC = reader.IsDBNull(reader.GetOrdinal("TemperatureC")) ? null : reader.GetDecimal(reader.GetOrdinal("TemperatureC")),
                RespiratoryRate = reader.IsDBNull(reader.GetOrdinal("RespiratoryRate")) ? null : reader.GetInt32(reader.GetOrdinal("RespiratoryRate")),
                OxygenSatPct = reader.IsDBNull(reader.GetOrdinal("OxygenSatPct")) ? null : reader.GetDecimal(reader.GetOrdinal("OxygenSatPct")),
                Ethnicity = reader.IsDBNull(reader.GetOrdinal("Ethnicity")) ? null : reader.GetString(reader.GetOrdinal("Ethnicity")),
                SmokingStatus = reader.IsDBNull(reader.GetOrdinal("SmokingStatus")) ? null : reader.GetString(reader.GetOrdinal("SmokingStatus")),
                TotalCholesterol = reader.IsDBNull(reader.GetOrdinal("TotalCholesterol")) ? null : reader.GetDecimal(reader.GetOrdinal("TotalCholesterol")),
                HDLCholesterol = reader.IsDBNull(reader.GetOrdinal("HDLCholesterol")) ? null : reader.GetDecimal(reader.GetOrdinal("HDLCholesterol")),
                DiabetesType = reader.IsDBNull(reader.GetOrdinal("DiabetesType")) ? null : reader.GetString(reader.GetOrdinal("DiabetesType")),
                FamilyCVDHistory = reader.IsDBNull(reader.GetOrdinal("FamilyCVDHistory")) ? null : reader.GetBoolean(reader.GetOrdinal("FamilyCVDHistory")),
                OnBPMedication = reader.IsDBNull(reader.GetOrdinal("OnBPMedication")) ? null : reader.GetBoolean(reader.GetOrdinal("OnBPMedication")),
                OnStatin = reader.IsDBNull(reader.GetOrdinal("OnStatin")) ? null : reader.GetBoolean(reader.GetOrdinal("OnStatin")),
                WaistCm = reader.IsDBNull(reader.GetOrdinal("WaistCm")) ? null : reader.GetDecimal(reader.GetOrdinal("WaistCm")),
                PhysicalActivity = reader.IsDBNull(reader.GetOrdinal("PhysicalActivity")) ? null : reader.GetBoolean(reader.GetOrdinal("PhysicalActivity")),
                EatsVegetablesDaily = reader.IsDBNull(reader.GetOrdinal("EatsVegetablesDaily")) ? null : reader.GetBoolean(reader.GetOrdinal("EatsVegetablesDaily")),
                HighBloodGlucoseHx = reader.IsDBNull(reader.GetOrdinal("HighBloodGlucoseHx")) ? null : reader.GetBoolean(reader.GetOrdinal("HighBloodGlucoseHx")),
                FamilyDiabetesHistory = reader.IsDBNull(reader.GetOrdinal("FamilyDiabetesHistory")) ? null : reader.GetBoolean(reader.GetOrdinal("FamilyDiabetesHistory")),
                Confusion = reader.IsDBNull(reader.GetOrdinal("Confusion")) ? null : reader.GetBoolean(reader.GetOrdinal("Confusion")),
                BloodUreaMmolPerL = reader.IsDBNull(reader.GetOrdinal("BloodUreaMmolPerL")) ? null : reader.GetDecimal(reader.GetOrdinal("BloodUreaMmolPerL"))
            });
        }

        return histories.ToList();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error retrieving patient histories for PatientId: {PatientId}", patientId);
        throw new Exception($"Failed to retrieve patient histories: {ex.Message}");
    }
}

        /// <summary>
        /// Gets all available doctors
        /// </summary>
        public async Task<List<DoctorProfileDto>> GetAllDoctorsAsync()
        {
            const string query = @"
                SELECT 
                    dp.DoctorId, dp.Specialty, dp.ConsultationFee,
                    u.FullName, u.PhoneNumber, u.ProfilePictureUrl
                FROM DoctorProfiles dp
                INNER JOIN Users u ON dp.DoctorId = u.UserId
                ORDER BY u.FullName";

            var doctors = new List<DoctorProfileDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                doctors.Add(new DoctorProfileDto
                {
                    DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                    Specialty = reader.IsDBNull(reader.GetOrdinal("Specialty")) ? null : reader.GetString(reader.GetOrdinal("Specialty")),
                    ConsultationFee = reader.IsDBNull(reader.GetOrdinal("ConsultationFee")) ? null : reader.GetDecimal(reader.GetOrdinal("ConsultationFee")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("ProfilePictureUrl")) ? null : reader.GetString(reader.GetOrdinal("ProfilePictureUrl"))
                });
            }

            return doctors;
        }

        /// <summary>
        /// Gets all doctor specialties
        /// </summary>
        public async Task<List<string>> GetDoctorSpecialtiesAsync()
        {
            const string query = @"
                SELECT DISTINCT Specialty 
                FROM DoctorProfiles 
                WHERE Specialty IS NOT NULL 
                ORDER BY Specialty";

            var specialties = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                specialties.Add(reader.GetString(reader.GetOrdinal("Specialty")));
            }

            return specialties;
        }

        /// <summary>
        /// Checks if a doctor's time slot is already taken
        /// </summary>
        public async Task<bool> IsDoctorSlotTakenAsync(int doctorId, DateTime appointmentDateTime)
        {
            const string query = @"
                SELECT COUNT(*)
                FROM Appointments
                WHERE DoctorId = @DoctorId 
                AND ScheduledDateTime = @ScheduledDateTime
                AND Status NOT IN ('Cancelled', 'Completed')";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DoctorId", doctorId);
            command.Parameters.AddWithValue("@ScheduledDateTime", appointmentDateTime);

            await connection.OpenAsync();
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }

        /// <summary>
        /// Gets all lab orders for a patient
        /// </summary>
        public async Task<List<LabOrderDto>> GetPatientLabOrdersAsync(int patientId)
        {
            const string query = @"
                SELECT 
                    lo.LabOrderId, lo.AppointmentId, lo.DoctorId, lo.PatientId, lo.LabTechId,
                    lo.OrderDate, lo.Status, lo.Results, lo.ReportFilePath, lo.CompletedAt,
                    d.FullName as DoctorName,
                    p.FullName as PatientName,
                    lt.FullName as LabTechName,
                    CASE 
                        WHEN lo.LabTechId IS NOT NULL THEN 'Assigned'
                        WHEN lo.Status = 'Pending' THEN 'Available'
                        ELSE 'Other'
                    END as AssignmentStatus
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE lo.PatientId = @PatientId
                ORDER BY lo.OrderDate DESC";

            var labOrders = new List<LabOrderDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                labOrders.Add(new LabOrderDto
                {
                    LabOrderId = reader.GetInt32(reader.GetOrdinal("LabOrderId")),
                    AppointmentId = reader.IsDBNull(reader.GetOrdinal("AppointmentId")) ? null : reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                    DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                    PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                    LabTechId = reader.IsDBNull(reader.GetOrdinal("LabTechId")) ? null : reader.GetInt32(reader.GetOrdinal("LabTechId")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Results = reader.IsDBNull(reader.GetOrdinal("Results")) ? null : reader.GetString(reader.GetOrdinal("Results")),
                    ReportFilePath = reader.IsDBNull(reader.GetOrdinal("ReportFilePath")) ? null : reader.GetString(reader.GetOrdinal("ReportFilePath")),
                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                    LabTechName = reader.IsDBNull(reader.GetOrdinal("LabTechName")) ? null : reader.GetString(reader.GetOrdinal("LabTechName")),
                    AssignmentStatus = reader.GetString(reader.GetOrdinal("AssignmentStatus"))
                });
            }

            return labOrders;
        }
        

        /// <summary>
        /// Gets submitted/completed lab orders for a patient
        /// </summary>
        public async Task<List<LabOrderDto>> GetSubmittedLabOrdersAsync(int patientId)
        {
            const string query = @"
                SELECT 
                    lo.LabOrderId, lo.AppointmentId, lo.DoctorId, lo.PatientId, lo.LabTechId,
                    lo.OrderDate, lo.Status, lo.Results, lo.ReportFilePath, lo.CompletedAt,
                    d.FullName as DoctorName,
                    p.FullName as PatientName,
                    lt.FullName as LabTechName,
                    'Assigned' as AssignmentStatus
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE lo.PatientId = @PatientId 
                AND lo.Status IN ('Completed', 'In Progress', 'Submitted')
                AND lo.LabTechId IS NOT NULL
                ORDER BY lo.CompletedAt DESC, lo.OrderDate DESC";

            var labOrders = new List<LabOrderDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                labOrders.Add(new LabOrderDto
                {
                    LabOrderId = reader.GetInt32(reader.GetOrdinal("LabOrderId")),
                    AppointmentId = reader.IsDBNull(reader.GetOrdinal("AppointmentId")) ? null : reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                    DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                    PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                    LabTechId = reader.IsDBNull(reader.GetOrdinal("LabTechId")) ? null : reader.GetInt32(reader.GetOrdinal("LabTechId")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Results = reader.IsDBNull(reader.GetOrdinal("Results")) ? null : reader.GetString(reader.GetOrdinal("Results")),
                    ReportFilePath = reader.IsDBNull(reader.GetOrdinal("ReportFilePath")) ? null : reader.GetString(reader.GetOrdinal("ReportFilePath")),
                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                    LabTechName = reader.IsDBNull(reader.GetOrdinal("LabTechName")) ? null : reader.GetString(reader.GetOrdinal("LabTechName")),
                    AssignmentStatus = reader.GetString(reader.GetOrdinal("AssignmentStatus"))
                });
            }

            return labOrders;
        }

        /// <summary>
        /// Gets a specific lab order by ID
        /// </summary>
        public async Task<LabOrderDto?> GetLabOrderByIdAsync(int labOrderId)
        {
            const string query = @"
                SELECT 
                    lo.LabOrderId, lo.AppointmentId, lo.DoctorId, lo.PatientId, lo.LabTechId,
                    lo.OrderDate, lo.Status, lo.Results, lo.ReportFilePath, lo.CompletedAt,
                    d.FullName as DoctorName,
                    p.FullName as PatientName,
                    lt.FullName as LabTechName,
                    CASE 
                        WHEN lo.LabTechId IS NOT NULL THEN 'Assigned'
                        WHEN lo.Status = 'Pending' THEN 'Available'
                        ELSE 'Other'
                    END as AssignmentStatus
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE lo.LabOrderId = @LabOrderId";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LabOrderId", labOrderId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new LabOrderDto
                {
                    LabOrderId = reader.GetInt32(reader.GetOrdinal("LabOrderId")),
                    AppointmentId = reader.IsDBNull(reader.GetOrdinal("AppointmentId")) ? null : reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                    DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                    PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                    LabTechId = reader.IsDBNull(reader.GetOrdinal("LabTechId")) ? null : reader.GetInt32(reader.GetOrdinal("LabTechId")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Results = reader.IsDBNull(reader.GetOrdinal("Results")) ? null : reader.GetString(reader.GetOrdinal("Results")),
                    ReportFilePath = reader.IsDBNull(reader.GetOrdinal("ReportFilePath")) ? null : reader.GetString(reader.GetOrdinal("ReportFilePath")),
                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                    LabTechName = reader.IsDBNull(reader.GetOrdinal("LabTechName")) ? null : reader.GetString(reader.GetOrdinal("LabTechName")),
                    AssignmentStatus = reader.GetString(reader.GetOrdinal("AssignmentStatus"))
                };
            }

            return null;
        }

        /// <summary>
        /// Gets lab orders for a patient by status
        /// </summary>
        public async Task<List<LabOrderDto>> GetLabOrdersByStatusAsync(int patientId, string status)
        {
            const string query = @"
                SELECT 
                    lo.LabOrderId, lo.AppointmentId, lo.DoctorId, lo.PatientId, lo.LabTechId,
                    lo.OrderDate, lo.Status, lo.Results, lo.ReportFilePath, lo.CompletedAt,
                    d.FullName as DoctorName,
                    p.FullName as PatientName,
                    lt.FullName as LabTechName,
                    CASE 
                        WHEN lo.LabTechId IS NOT NULL THEN 'Assigned'
                        WHEN lo.Status = 'Pending' THEN 'Available'
                        ELSE 'Other'
                    END as AssignmentStatus
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE lo.PatientId = @PatientId AND lo.Status = @Status
                ORDER BY lo.OrderDate DESC";

            var labOrders = new List<LabOrderDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PatientId", patientId);
            command.Parameters.AddWithValue("@Status", status);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                labOrders.Add(new LabOrderDto
                {
                    LabOrderId = reader.GetInt32(reader.GetOrdinal("LabOrderId")),
                    AppointmentId = reader.IsDBNull(reader.GetOrdinal("AppointmentId")) ? null : reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                    DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                    PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                    LabTechId = reader.IsDBNull(reader.GetOrdinal("LabTechId")) ? null : reader.GetInt32(reader.GetOrdinal("LabTechId")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Results = reader.IsDBNull(reader.GetOrdinal("Results")) ? null : reader.GetString(reader.GetOrdinal("Results")),
                    ReportFilePath = reader.IsDBNull(reader.GetOrdinal("ReportFilePath")) ? null : reader.GetString(reader.GetOrdinal("ReportFilePath")),
                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName")),
                    LabTechName = reader.IsDBNull(reader.GetOrdinal("LabTechName")) ? null : reader.GetString(reader.GetOrdinal("LabTechName")),
                    AssignmentStatus = reader.GetString(reader.GetOrdinal("AssignmentStatus"))
                });
            }

            return labOrders;
        }
    }
}