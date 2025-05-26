using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using System.Transactions;
using System.Security.Cryptography;
using System.Text;

 // Added this using directive

namespace BlazorApp1.Services.Implementations
{
    public class DoctorService : IDoctorService
    {
        private readonly string _connectionString;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DoctorService> _logger;

        public DoctorService(IConfiguration configuration, HttpClient httpClient, ILogger<DoctorService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient;
            _logger = logger;
        }

    public async Task<int> AddPatientHistoryAsync(PatientHistoryDto dto)
{
    const string sql = @"
        INSERT INTO dbo.PatientHistory
            (PatientId, DoctorId, EncounterDate,
             Symptoms, Notes,
             WeightKg, HeightCm, BMI,
             PulseBPM, BloodPressure, TemperatureC,
             RespiratoryRate, OxygenSatPct)
        VALUES
            (@PatientId, @DoctorId, @EncounterDate,
             @Symptoms, @Notes,
             @WeightKg, @HeightCm, @BMI,
             @PulseBPM, @BloodPressure, @TemperatureC,
             @RespiratoryRate, @OxygenSatPct);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";

    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@PatientId", dto.PatientId);
    cmd.Parameters.AddWithValue("@DoctorId", dto.DoctorId);
    cmd.Parameters.AddWithValue("@EncounterDate", dto.EncounterDate);

    // If any field is null, pass DBNull.Value
    cmd.Parameters.AddWithValue("@Symptoms", (object?)dto.Symptoms ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@Notes", (object?)dto.Notes ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@WeightKg", (object?)dto.WeightKg ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@HeightCm", (object?)dto.HeightCm ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@BMI", (object?)dto.BMI ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@PulseBPM", (object?)dto.PulseBPM ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@BloodPressure", (object?)dto.BloodPressure ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@TemperatureC", (object?)dto.TemperatureC ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@RespiratoryRate", (object?)dto.RespiratoryRate ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@OxygenSatPct", (object?)dto.OxygenSatPct ?? DBNull.Value);

    var result = await cmd.ExecuteScalarAsync();
    return Convert.ToInt32(result);
}
 public async Task<IEnumerable<PatientHistoryDto>> GetPatientHistoryAsync(int patientId)
    {
        const string sql = @"
            SELECT 
                HistoryId,
                PatientId,
                DoctorId,
                EncounterDate,
                Symptoms,
                Notes,
                WeightKg,
                HeightCm,
                BMI,
                PulseBPM,
                BloodPressure,
                TemperatureC,
                RespiratoryRate,
                OxygenSatPct
            FROM dbo.PatientHistory
            WHERE PatientId = @PatientId
            ORDER BY EncounterDate DESC;
        ";

        var historyList = new List<PatientHistoryDto>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PatientId", patientId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            historyList.Add(new PatientHistoryDto
            {
                HistoryId       = reader.GetInt32(reader.GetOrdinal("HistoryId")),
                PatientId       = reader.GetInt32(reader.GetOrdinal("PatientId")),
                DoctorId        = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                EncounterDate   = reader.GetDateTime(reader.GetOrdinal("EncounterDate")),
                Symptoms        = reader.IsDBNull(reader.GetOrdinal("Symptoms"))
                                     ? null 
                                     : reader.GetString(reader.GetOrdinal("Symptoms")),
                Notes           = reader.IsDBNull(reader.GetOrdinal("Notes"))
                                     ? null 
                                     : reader.GetString(reader.GetOrdinal("Notes")),
                WeightKg        = reader.IsDBNull(reader.GetOrdinal("WeightKg"))
                                     ? (decimal?)null 
                                     : reader.GetDecimal(reader.GetOrdinal("WeightKg")),
                HeightCm        = reader.IsDBNull(reader.GetOrdinal("HeightCm"))
                                     ? (decimal?)null 
                                     : reader.GetDecimal(reader.GetOrdinal("HeightCm")),
                BMI             = reader.IsDBNull(reader.GetOrdinal("BMI"))
                                     ? (decimal?)null 
                                     : reader.GetDecimal(reader.GetOrdinal("BMI")),
                PulseBPM        = reader.IsDBNull(reader.GetOrdinal("PulseBPM"))
                                     ? (int?)null 
                                     : reader.GetInt32(reader.GetOrdinal("PulseBPM")),
                BloodPressure   = reader.IsDBNull(reader.GetOrdinal("BloodPressure"))
                                     ? null 
                                     : reader.GetString(reader.GetOrdinal("BloodPressure")),
                TemperatureC    = reader.IsDBNull(reader.GetOrdinal("TemperatureC"))
                                     ? (decimal?)null 
                                     : reader.GetDecimal(reader.GetOrdinal("TemperatureC")),
                RespiratoryRate = reader.IsDBNull(reader.GetOrdinal("RespiratoryRate"))
                                     ? (int?)null 
                                     : reader.GetInt32(reader.GetOrdinal("RespiratoryRate")),
                OxygenSatPct    = reader.IsDBNull(reader.GetOrdinal("OxygenSatPct"))
                                     ? (decimal?)null 
                                     : reader.GetDecimal(reader.GetOrdinal("OxygenSatPct"))
            });
        }

        return historyList;
    }
    
public async Task UpdatePatientHistoryAsync(PatientHistoryDto dto)
        {
            const string sql = @"
        UPDATE dbo.PatientHistory
        SET 
            EncounterDate   = @EncounterDate,
            Symptoms        = @Symptoms,
            Notes           = @Notes,
            WeightKg        = @WeightKg,
            HeightCm        = @HeightCm,
            BMI             = @BMI,
            PulseBPM        = @PulseBPM,
            BloodPressure   = @BloodPressure,
            TemperatureC    = @TemperatureC,
            RespiratoryRate = @RespiratoryRate,
            OxygenSatPct    = @OxygenSatPct
        WHERE HistoryId = @HistoryId;
    ";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HistoryId", dto.HistoryId);
            cmd.Parameters.AddWithValue("@EncounterDate", dto.EncounterDate);

            // For each nullable field, use DBNull if null:
            cmd.Parameters.AddWithValue("@Symptoms", (object?)dto.Symptoms ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)dto.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WeightKg", (object?)dto.WeightKg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HeightCm", (object?)dto.HeightCm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BMI", (object?)dto.BMI ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PulseBPM", (object?)dto.PulseBPM ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BloodPressure", (object?)dto.BloodPressure ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TemperatureC", (object?)dto.TemperatureC ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RespiratoryRate", (object?)dto.RespiratoryRate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OxygenSatPct", (object?)dto.OxygenSatPct ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DoctorProfileDto?> GetDoctorProfileAsync(int doctorId)
        {
            const string sql = @"
                SELECT 
                    dp.DoctorId,
                    dp.Specialty,
                    dp.Qualifications,
                    u.UserName,
                    u.CNIC,
                    u.PhoneNumber,
                    u.FullName,
                    u.ProfilePictureUrl
                FROM dbo.DoctorProfiles dp
                INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
                WHERE dp.DoctorId = @DoctorId";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = doctorId;
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new DoctorProfileDto
                    {
                        DoctorId = reader.GetInt32("DoctorId"),
                        Specialty = reader.IsDBNull("Specialty") ? null : reader.GetString("Specialty"),
                        Qualifications = reader.IsDBNull("Qualifications") ? null : reader.GetString("Qualifications"),
                        UserName = reader.GetString("UserName"),
                        CNIC = reader.GetString("CNIC"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        FullName = reader.GetString("FullName"),
                        ProfilePictureUrl = reader.IsDBNull("ProfilePictureUrl") ? null : reader.GetString("ProfilePictureUrl")
                    };
                }
                
                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting doctor profile for DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException("Error retrieving doctor profile", ex);
            }
        }

        public async Task<DoctorProfileDto?> GetDoctorProfileByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT 
                    dp.DoctorId,
                    dp.Specialty,
                    dp.Qualifications,
                    u.UserName,
                    u.CNIC,
                    u.PhoneNumber,
                    u.FullName,
                    u.ProfilePictureUrl
                FROM dbo.DoctorProfiles dp
                INNER JOIN dbo.Users u ON dp.DoctorId = u.UserId
                WHERE u.UserId = @UserId";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new DoctorProfileDto
                    {
                        DoctorId = reader.GetInt32("DoctorId"),
                        Specialty = reader.IsDBNull("Specialty") ? null : reader.GetString("Specialty"),
                        Qualifications = reader.IsDBNull("Qualifications") ? null : reader.GetString("Qualifications"),
                        UserName = reader.GetString("UserName"),
                        CNIC = reader.GetString("CNIC"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        FullName = reader.GetString("FullName"),
                        ProfilePictureUrl = reader.IsDBNull("ProfilePictureUrl") ? null : reader.GetString("ProfilePictureUrl")
                    };
                }
                
                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting doctor profile for UserId: {UserId}", userId);
                throw new InvalidOperationException("Error retrieving doctor profile", ex);
            }
        }

        // ==============================================================================
        // 2. LIST ASSIGNED PATIENTS
        // ==============================================================================
        public async Task<IEnumerable<PatientSummaryDto>> GetAssignedPatientsAsync(int doctorId)
        {
            const string sql = @"
                SELECT DISTINCT
                    pp.PatientId,
                    u.UserId,
                    u.FullName,
                    u.CNIC,
                    u.PhoneNumber,
                    pp.DateOfBirth,
                    pp.Gender,
                    pp.Address,
                    pp.BloodGroup,
                    u.CreatedAt,
                    COUNT(a.AppointmentId) as TotalAppointments,
                    MAX(a.ScheduledDateTime) as LastAppointmentDate
                FROM dbo.PatientProfiles pp
                INNER JOIN dbo.Users u ON pp.PatientId = u.UserId
                INNER JOIN dbo.Appointments a ON pp.PatientId = a.PatientId
                WHERE a.DoctorId = @DoctorId
                GROUP BY pp.PatientId, u.UserId, u.FullName, u.CNIC, u.PhoneNumber, 
                         pp.DateOfBirth, pp.Gender, pp.Address, pp.BloodGroup, u.CreatedAt
                ORDER BY MAX(a.ScheduledDateTime) DESC";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = doctorId;
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var patients = new List<PatientSummaryDto>();
                while (await reader.ReadAsync())
                {
                    patients.Add(new PatientSummaryDto
                    {
                        PatientId = reader.GetInt32("PatientId"),
                        UserId = reader.GetInt32("UserId"),
                        FullName = reader.GetString("FullName"),
                        CNIC = reader.GetString("CNIC"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        DateOfBirth = reader.IsDBNull("DateOfBirth") ? null : reader.GetDateTime("DateOfBirth"),
                        Gender = reader.IsDBNull("Gender") ? null : reader.GetString("Gender"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        BloodGroup = reader.IsDBNull("BloodGroup") ? null : reader.GetString("BloodGroup"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        TotalAppointments = reader.GetInt32("TotalAppointments"),
                        LastAppointmentDate = reader.IsDBNull("LastAppointmentDate") ? null : reader.GetDateTime("LastAppointmentDate")
                    });
                }
                
                return patients;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting assigned patients for DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException("Error retrieving assigned patients", ex);
            }
        }

        // ==============================================================================
        // 3. GET APPOINTMENTS
        // ==============================================================================
        public async Task<IEnumerable<AppointmentDto>> GetDoctorAppointmentsAsync(int doctorId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var sql = @"
                SELECT 
                    a.AppointmentId,
                    a.DoctorId,
                    a.PatientId,
                    a.ScheduledDateTime,
                    a.Status,
                    a.CreatedAt,
                    a.UpdatedAt,
                    pu.FullName as PatientName,
                    du.FullName as DoctorName,
                    pu.PhoneNumber as PatientPhone,
                    pp.Gender as PatientGender,
                    CASE 
                        WHEN pp.DateOfBirth IS NOT NULL 
                        THEN DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()) - 
                             CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, pp.DateOfBirth, GETDATE()), pp.DateOfBirth) > GETDATE() 
                                  THEN 1 ELSE 0 END
                        ELSE NULL 
                    END as PatientAge
                FROM dbo.Appointments a
                INNER JOIN dbo.PatientProfiles pp ON a.PatientId = pp.PatientId
                INNER JOIN dbo.Users pu ON pp.PatientId = pu.UserId
                INNER JOIN dbo.DoctorProfiles dp ON a.DoctorId = dp.DoctorId
                INNER JOIN dbo.Users du ON dp.DoctorId = du.UserId
                WHERE a.DoctorId = @DoctorId";

            var parameters = new List<SqlParameter>
            {
                new("@DoctorId", SqlDbType.Int) { Value = doctorId }
            };

            if (fromDate.HasValue)
            {
                sql += " AND a.ScheduledDateTime >= @FromDate";
                parameters.Add(new("@FromDate", SqlDbType.DateTime2) { Value = fromDate.Value });
            }

            if (toDate.HasValue)
            {
                sql += " AND a.ScheduledDateTime <= @ToDate";
                parameters.Add(new("@ToDate", SqlDbType.DateTime2) { Value = toDate.Value });
            }

            sql += " ORDER BY a.ScheduledDateTime ASC";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var appointments = new List<AppointmentDto>();
                while (await reader.ReadAsync())
                {
                    appointments.Add(new AppointmentDto
                    {
                        AppointmentId = reader.GetInt32("AppointmentId"),
                        DoctorId = reader.GetInt32("DoctorId"),
                        PatientId = reader.GetInt32("PatientId"),
                        ScheduledDateTime = reader.GetDateTime("ScheduledDateTime"),
                        Status = reader.GetString("Status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt"),
                        PatientName = reader.GetString("PatientName"),
                        DoctorName = reader.GetString("DoctorName"),
                        PatientPhone = reader.IsDBNull("PatientPhone") ? null : reader.GetString("PatientPhone"),
                        PatientAge = reader.IsDBNull("PatientAge") ? null : reader.GetInt32("PatientAge"),
                        PatientGender = reader.IsDBNull("PatientGender") ? null : reader.GetString("PatientGender")
                    });
                }
                
                return appointments;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting appointments for DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException("Error retrieving appointments", ex);
            }
        }

        public async Task<IEnumerable<AppointmentDto>> GetTodayAppointmentsAsync(int doctorId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await GetDoctorAppointmentsAsync(doctorId, today, tomorrow);
        }

   
        public async Task<int> AddDoctorNoteAsync(int doctorId, CreateDoctorNoteRequest request)
        {
            const string sql = @"
                INSERT INTO dbo.DoctorNotes (AppointmentId, DoctorId, PatientId, NoteText, CreatedAt)
                VALUES (@AppointmentId, @DoctorId, @PatientId, @NoteText, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@AppointmentId", SqlDbType.Int).Value = (object?)request.AppointmentId ?? DBNull.Value;
                command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = doctorId;
                command.Parameters.Add("@PatientId", SqlDbType.Int).Value = request.PatientId;
                command.Parameters.Add("@NoteText", SqlDbType.NVarChar, -1).Value = request.NoteText;
                command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                
                await connection.OpenAsync();
                var noteId = await command.ExecuteScalarAsync();
                
                return Convert.ToInt32(noteId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error adding doctor note for DoctorId: {DoctorId}, PatientId: {PatientId}", 
                    doctorId, request.PatientId);
                throw new InvalidOperationException("Error adding doctor note", ex);
            }
        }

 public async Task ChangePasswordAsync(ChangeDoctorPasswordRequest dto)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // 1) Verify the current password
        const string verifyQuery = @"
            SELECT PasswordHash 
            FROM dbo.Users 
            WHERE UserId = @DoctorId";
        using var verifyCommand = new SqlCommand(verifyQuery, connection);
        verifyCommand.Parameters.AddWithValue("@DoctorId", dto.DoctorId);

        var currentHashObj = await verifyCommand.ExecuteScalarAsync();
        if (currentHashObj == null)
        {
            throw new InvalidOperationException("Doctor not found.");
        }

        var currentHash = currentHashObj.ToString()!;
        if (!VerifyPassword(dto.CurrentPassword, currentHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // 2) Hash the new password
        var newPasswordHash = HashPassword(dto.NewPassword);

        // 3) Update the password
        const string updateQuery = @"
            UPDATE dbo.Users
            SET 
                PasswordHash = @NewPasswordHash,
                UpdatedAt    = SYSUTCDATETIME()
            WHERE UserId = @DoctorId";
        using var updateCommand = new SqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@DoctorId", dto.DoctorId);
        updateCommand.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("Failed to update password.");
        }

        _logger.LogInformation("Password changed successfully for doctor ID: {DoctorId}", dto.DoctorId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error changing password for doctor ID: {DoctorId}", dto.DoctorId);
        throw;
    }
}

         private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
          private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
        public async Task<IEnumerable<DoctorNoteDto>> GetDoctorNotesAsync(int doctorId, int? patientId = null)
        {
            var sql = @"
                SELECT 
                    dn.NoteId,
                    dn.AppointmentId,
                    dn.DoctorId,
                    dn.PatientId,
                    dn.NoteText,
                    dn.CreatedAt,
                    du.FullName as DoctorName,
                    pu.FullName as PatientName
                FROM dbo.DoctorNotes dn
                INNER JOIN dbo.DoctorProfiles dp ON dn.DoctorId = dp.DoctorId
                INNER JOIN dbo.Users du ON dp.DoctorId = du.UserId
                INNER JOIN dbo.PatientProfiles pp ON dn.PatientId = pp.PatientId
                INNER JOIN dbo.Users pu ON pp.PatientId = pu.UserId
                WHERE dn.DoctorId = @DoctorId";

            var parameters = new List<SqlParameter>
            {
                new("@DoctorId", SqlDbType.Int) { Value = doctorId }
            };

            if (patientId.HasValue)
            {
                sql += " AND dn.PatientId = @PatientId";
                parameters.Add(new("@PatientId", SqlDbType.Int) { Value = patientId.Value });
            }

            sql += " ORDER BY dn.CreatedAt DESC";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);

                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                var notes = new List<DoctorNoteDto>();
                while (await reader.ReadAsync())
                {
                    notes.Add(new DoctorNoteDto
                    {
                        NoteId = reader.GetInt32("NoteId"),
                        AppointmentId = reader.IsDBNull("AppointmentId") ? null : reader.GetInt32("AppointmentId"),
                        DoctorId = reader.GetInt32("DoctorId"),
                        PatientId = reader.GetInt32("PatientId"),
                        NoteText = reader.GetString("NoteText"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        DoctorName = reader.GetString("DoctorName"),
                        PatientName = reader.GetString("PatientName")
                    });
                }

                return notes;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting doctor notes for DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException("Error retrieving doctor notes", ex);
            }
        }


        public async Task<int> CreateLabOrderAsync(int doctorId, CreateLabOrderRequest request)
        {
            const string sql = @"
                INSERT INTO dbo.LabOrders (AppointmentId, DoctorId, PatientId, OrderDate, Status)
                VALUES (@AppointmentId, @DoctorId, @PatientId, @OrderDate, 'Pending');
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@AppointmentId", SqlDbType.Int).Value = (object?)request.AppointmentId ?? DBNull.Value;
                command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = doctorId;
                command.Parameters.Add("@PatientId", SqlDbType.Int).Value = request.PatientId;
                command.Parameters.Add("@OrderDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                
                await connection.OpenAsync();
                var labOrderId = await command.ExecuteScalarAsync();
                
                return Convert.ToInt32(labOrderId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error creating lab order for DoctorId: {DoctorId}, PatientId: {PatientId}", 
                    doctorId, request.PatientId);
                throw new InvalidOperationException("Error creating lab order", ex);
            }
        }


        public async Task<IEnumerable<LabOrderDto>> GetDoctorLabOrdersAsync(int doctorId, string? status = null)
        {
            var sql = @"
                SELECT 
                    lo.LabOrderId,
                    lo.DoctorId,
                    lo.PatientId,
                    lo.AppointmentId,
                    lo.OrderDate,
                    lo.Status,
                    lo.Results,
                    lo.ReportFilePath,
                    lo.CompletedAt,
                    du.FullName as DoctorName,
                    pu.FullName as PatientName,
                    ltu.FullName as LabTechName
                FROM dbo.LabOrders lo
                INNER JOIN dbo.DoctorProfiles dp ON lo.DoctorId = dp.DoctorId
                INNER JOIN dbo.Users du ON dp.DoctorId = du.UserId
                INNER JOIN dbo.PatientProfiles pp ON lo.PatientId = pp.PatientId
                INNER JOIN dbo.Users pu ON pp.PatientId = pu.UserId
                LEFT JOIN dbo.LabTechnicianProfiles ltp ON lo.LabTechId = ltp.LabTechId
                LEFT JOIN dbo.Users ltu ON ltp.LabTechId = ltu.UserId
                WHERE lo.DoctorId = @DoctorId";

            var parameters = new List<SqlParameter>
            {
                new("@DoctorId", SqlDbType.Int) { Value = doctorId }
            };

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND lo.Status = @Status";
                parameters.Add(new("@Status", SqlDbType.NVarChar, 20) { Value = status });
            }

            sql += " ORDER BY lo.OrderDate DESC";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var labOrders = new List<LabOrderDto>();
                while (await reader.ReadAsync())
                {
                    labOrders.Add(new LabOrderDto
                    {
                        LabOrderId = reader.GetInt32("LabOrderId"),
                        DoctorId = reader.GetInt32("DoctorId"),
                        PatientId = reader.GetInt32("PatientId"),
                        AppointmentId = reader.IsDBNull("AppointmentId") ? null : reader.GetInt32("AppointmentId"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        Status = reader.GetString("Status"),
                        Results = reader.IsDBNull("Results") ? null : reader.GetString("Results"),
                        ReportFilePath = reader.IsDBNull("ReportFilePath") ? null : reader.GetString("ReportFilePath"),
                        CompletedAt = reader.IsDBNull("CompletedAt") ? null : reader.GetDateTime("CompletedAt"),
                        DoctorName = reader.GetString("DoctorName"),
                        PatientName = reader.GetString("PatientName"),
                        LabTechName = reader.IsDBNull("LabTechName") ? null : reader.GetString("LabTechName")
                    });
                }
                
                return labOrders;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting lab orders for DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException("Error retrieving lab orders", ex);
            }
        }

        public async Task<int> CreateAIPredictionAsync(int doctorId, AIPredictionRequest request)
        {
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            try
            {
                // Call external AI service
                var aiResponse = await CallAIPredictionServiceAsync(request);
                
                // Save to database
                const string sql = @"
                    INSERT INTO dbo.AIPredictions (DoctorId, PatientId, PredictionType, Data, CreatedAt)
                    VALUES (@DoctorId, @PatientId, @PredictionType, @Data, @CreatedAt);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@DoctorId", SqlDbType.Int).Value = doctorId;
                command.Parameters.Add("@PatientId", SqlDbType.Int).Value = request.PatientId;
                command.Parameters.Add("@PredictionType", SqlDbType.NVarChar, 20).Value = request.PredictionType;
                command.Parameters.Add("@Data", SqlDbType.NVarChar, -1).Value = aiResponse;
                command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                
                await connection.OpenAsync();
                var predictionId = await command.ExecuteScalarAsync();
                
                transaction.Complete();
                return Convert.ToInt32(predictionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AI prediction for DoctorId: {DoctorId}, PatientId: {PatientId}", 
                    doctorId, request.PatientId);
                throw new InvalidOperationException("Error creating AI prediction", ex);
            }
        }

        private async Task<string> CallAIPredictionServiceAsync(AIPredictionRequest request)
        {
            try
            {
                var requestData = new
                {
                    patientId = request.PatientId,
                    predictionType = request.PredictionType,
                    inputData = request.InputData
                };

                var jsonContent = JsonSerializer.Serialize(requestData);
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Replace with your actual AI service endpoint
                var response = await _httpClient.PostAsync("/api/ai/predict", httpContent);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI prediction service");
                throw new InvalidOperationException("AI service unavailable", ex);
            }
        }

    public async Task MarkNotificationAsReadAsync(int notificationId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                UPDATE dbo.Notifications
                SET IsRead = 1
                WHERE NotificationId = @NotificationId;
            ";

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

    // 2) Mark all notifications as read for a given doctor (user)
    public async Task MarkAllNotificationsAsReadAsync(int doctorId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                UPDATE dbo.Notifications
                SET IsRead = 1
                WHERE UserId = @DoctorId AND IsRead = 0;
            ";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DoctorId", doctorId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Marked {Count} notifications as read for doctor: {DoctorId}", 
                                    rowsAffected, doctorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for doctor: {DoctorId}", doctorId);
            throw;
        }
    }

    // 3) Retrieve the most recent notifications for a given doctor
    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int doctorId)
    {
        var notifications = new List<NotificationDto>();

        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT NotificationId, UserId, Title, Message, IsRead, CreatedAt
                FROM dbo.Notifications
                WHERE UserId = @DoctorId
                ORDER BY CreatedAt DESC;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DoctorId", doctorId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notifications.Add(new NotificationDto
                {
                    NotificationId = reader.GetInt32(reader.GetOrdinal("NotificationId")),
                    UserId         = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Title          = reader.IsDBNull(reader.GetOrdinal("Title")) 
                                       ? null 
                                       : reader.GetString(reader.GetOrdinal("Title")),
                    Message        = reader.IsDBNull(reader.GetOrdinal("Message")) 
                                       ? null 
                                       : reader.GetString(reader.GetOrdinal("Message")),
                    IsRead         = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                    CreatedAt      = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for doctor: {DoctorId}", doctorId);
            throw;
        }

        return notifications;
    }

        public async Task<IEnumerable<AIPredictionDto>> GetAIPredictionsAsync(int doctorId, int? patientId = null)
        {
            var sql = @"
                SELECT 
                    ap.PredictionId,
                    ap.DoctorId,
                    ap.PatientId,
                    ap.PredictionType,
                    ap.Data,
                    ap.CreatedAt,
                    du.FullName as DoctorName,
                    pu.FullName as PatientName
                FROM dbo.AIPredictions ap
                INNER JOIN dbo.DoctorProfiles dp ON ap.DoctorId = dp.DoctorId
                INNER JOIN dbo.Users du ON dp.DoctorId = du.UserId
                LEFT JOIN dbo.PatientProfiles pp ON ap.PatientId = pp.PatientId
                LEFT JOIN dbo.Users pu ON pp.PatientId = pu.UserId
                WHERE ap.DoctorId = @DoctorId";

            var parameters = new List<SqlParameter>
            {
                new("@DoctorId", SqlDbType.Int) { Value = doctorId }
            };

            if (patientId.HasValue)
            {
                sql += " AND ap.PatientId = @PatientId";
                parameters.Add(new("@PatientId", SqlDbType.Int) { Value = patientId.Value });
            }

            sql += " ORDER BY ap.CreatedAt DESC";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                var predictions = new List<AIPredictionDto>();
                while (await reader.ReadAsync())
                {
                    predictions.Add(new AIPredictionDto
                    {
                        PredictionId = reader.GetInt32("PredictionId"),
                        DoctorId = reader.IsDBNull("DoctorId") ? null : reader.GetInt32("DoctorId"),
                        PatientId = reader.IsDBNull("PatientId") ? null : reader.GetInt32("PatientId"),
                        PredictionType = reader.GetString("PredictionType"),
                        Data = reader.GetString("Data"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        DoctorName = reader.IsDBNull("DoctorName") ? null : reader.GetString("DoctorName"),
                        PatientName = reader.IsDBNull("PatientName") ? null : reader.GetString("PatientName")
                    });
                }
                
                return predictions;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting AI predictions for DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException("Error retrieving AI predictions", ex);
            }
        }

      // In DoctorService.cs (inside the DoctorService class)
public async Task UpdateDoctorProfileAsync(int doctorId, UpdateDoctorProfileRequest request)
{
    // request contains: FullName, PhoneNumber, ProfilePictureUrl
    const string sql = @"
        -- Only update Users because Specialty/Qualifications are read-only here
        UPDATE dbo.Users
        SET 
            FullName          = @FullName,
            PhoneNumber       = @PhoneNumber,
            ProfilePictureUrl = @ProfilePictureUrl,
            UpdatedAt         = SYSUTCDATETIME()
        WHERE UserId = @DoctorId;
    ";

    try
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@FullName", (object)request.FullName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PhoneNumber", (object?)request.PhoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProfilePictureUrl", (object?)request.ProfilePictureUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DoctorId", doctorId);

        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        // Log the full exception for diagnostics
        _logger.LogError(ex, "Database error updating doctor profile for DoctorId: {DoctorId}", doctorId);
        throw; // rethrow so the caller sees the error
    }
}


    
        public async Task<PatientSummaryDto?> GetPatientSummaryAsync(int patientId)
        {
            const string sql = @"
                SELECT 
                    pp.PatientId,
                    u.UserId,
                    u.FullName,
                    u.CNIC,
                    u.PhoneNumber,
                    pp.DateOfBirth,
                    pp.Gender,
                    pp.Address,
                    pp.BloodGroup,
                    u.CreatedAt,
                    COUNT(a.AppointmentId) as TotalAppointments,
                    MAX(a.ScheduledDateTime) as LastAppointmentDate
                FROM dbo.PatientProfiles pp
                INNER JOIN dbo.Users u ON pp.PatientId = u.UserId
                LEFT JOIN dbo.Appointments a ON pp.PatientId = a.PatientId
                WHERE pp.PatientId = @PatientId
                GROUP BY pp.PatientId, u.UserId, u.FullName, u.CNIC, u.PhoneNumber, 
                         pp.DateOfBirth, pp.Gender, pp.Address, pp.BloodGroup, u.CreatedAt";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);
                
                command.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new PatientSummaryDto
                    {
                        PatientId = reader.GetInt32("PatientId"),
                        UserId = reader.GetInt32("UserId"),
                        FullName = reader.GetString("FullName"),
                        CNIC = reader.GetString("CNIC"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
                        DateOfBirth = reader.IsDBNull("DateOfBirth") ? null : reader.GetDateTime("DateOfBirth"),
                        Gender = reader.IsDBNull("Gender") ? null : reader.GetString("Gender"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        BloodGroup = reader.IsDBNull("BloodGroup") ? null : reader.GetString("BloodGroup"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        TotalAppointments = reader.GetInt32("TotalAppointments"),
                        LastAppointmentDate = reader.IsDBNull("LastAppointmentDate") ? null : reader.GetDateTime("LastAppointmentDate")
                    };
                }
                
                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error getting patient summary for PatientId: {PatientId}", patientId);
                throw new InvalidOperationException("Error retrieving patient summary", ex);
            }
        }
    }
}