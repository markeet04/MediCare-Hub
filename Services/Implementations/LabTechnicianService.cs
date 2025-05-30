using BlazorApp1.Models.DTOs;
using BlazorApp1.Services;
using Microsoft.AspNetCore.Http;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp1.Services
{
    public class LabTechnicianService : ILabTechnicianService
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public LabTechnicianService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string not found");
            _environment = environment;
        }

        public async Task<LabTechnicianDashboardStatsDto> GetDashboardStatsAsync(int labTechId)
        {
            var stats = new LabTechnicianDashboardStatsDto();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get available orders (unassigned)
            var availableQuery = @"
                SELECT COUNT(*) 
                FROM LabOrders 
                WHERE LabTechId IS NULL AND Status = 'Pending'";

            using (var cmd = new SqlCommand(availableQuery, connection))
            {
                stats.PendingOrders = (int)await cmd.ExecuteScalarAsync();
            }

            // Get assigned orders for this tech that are in progress
            var assignedQuery = @"
                SELECT COUNT(*) 
                FROM LabOrders 
                WHERE LabTechId = @LabTechId AND Status = 'InProgress'";

            using (var cmd = new SqlCommand(assignedQuery, connection))
            {
                cmd.Parameters.AddWithValue("@LabTechId", labTechId);
                var assignedCount = (int)await cmd.ExecuteScalarAsync();
                stats.PendingOrders += assignedCount; // Total pending includes available + assigned in progress
            }

            // Get completed today
            var completedTodayQuery = @"
                SELECT COUNT(*) 
                FROM LabOrders 
                WHERE LabTechId = @LabTechId 
                    AND Status IN ('Completed', 'Reported') 
                    AND CAST(CompletedAt AS DATE) = CAST(GETDATE() AS DATE)";

            using (var cmd = new SqlCommand(completedTodayQuery, connection))
            {
                cmd.Parameters.AddWithValue("@LabTechId", labTechId);
                stats.CompletedToday = (int)await cmd.ExecuteScalarAsync();
            }

            // Get total completed
            var totalCompletedQuery = @"
                SELECT COUNT(*) 
                FROM LabOrders 
                WHERE LabTechId = @LabTechId AND Status IN ('Completed', 'Reported')";

            using (var cmd = new SqlCommand(totalCompletedQuery, connection))
            {
                cmd.Parameters.AddWithValue("@LabTechId", labTechId);
                stats.TotalCompleted = (int)await cmd.ExecuteScalarAsync();
            }

            // Get overdue orders (older than 2 days and still pending/in progress, either unassigned or assigned to this tech)
            var overdueQuery = @"
                SELECT COUNT(*) 
                FROM LabOrders 
                WHERE (LabTechId IS NULL OR LabTechId = @LabTechId)
                    AND Status IN ('Pending', 'InProgress') 
                    AND DATEDIFF(day, OrderDate, GETDATE()) > 2";

            using (var cmd = new SqlCommand(overdueQuery, connection))
            {
                cmd.Parameters.AddWithValue("@LabTechId", labTechId);
                stats.OverdueOrders = (int)await cmd.ExecuteScalarAsync();
            }

            return stats;
        }



        public async Task<List<LabOrderDto>> GetAvailableAndAssignedOrdersAsync(int labTechId)
        {
            var orders = new List<LabOrderDto>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT lo.*, 
                       d.FullName as DoctorName,
                       p.FullName as PatientName,
                       lt.FullName as LabTechName
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE (lo.LabTechId IS NULL AND lo.Status = 'Pending') 
                   OR (lo.LabTechId = @LabTechId AND lo.Status IN ('InProgress', 'Pending'))
                ORDER BY 
                    CASE WHEN lo.LabTechId = @LabTechId THEN 0 ELSE 1 END, -- Assigned orders first
                    lo.OrderDate ASC"; // Oldest orders first

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@LabTechId", labTechId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var order = MapLabOrderFromReader(reader);

                // Set assignment status
                if (order.LabTechId == labTechId)
                {
                    order.AssignmentStatus = "Assigned";
                }
                else if (order.LabTechId == null)
                {
                    order.AssignmentStatus = "Available";
                }
                else
                {
                    order.AssignmentStatus = "Other";
                }

                orders.Add(order);
            }

            return orders;
        }

        public async Task<bool> ClaimLabOrderAsync(int orderId, int labTechId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE LabOrders 
                SET LabTechId = @LabTechId,
                    Status = 'InProgress'
                WHERE LabOrderId = @OrderId 
                    AND LabTechId IS NULL 
                    AND Status = 'Pending'";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@LabTechId", labTechId);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<LabOrderDto?> GetLabOrderByIdAsync(int orderId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT lo.*, 
                       d.FullName as DoctorName,
                       p.FullName as PatientName,
                       lt.FullName as LabTechName
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE lo.LabOrderId = @OrderId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var order = MapLabOrderFromReader(reader);

                // Set assignment status (this will be determined by the calling context)
                if (order.LabTechId != null)
                {
                    order.AssignmentStatus = "Assigned"; // Could be "Other" depending on context
                }
                else
                {
                    order.AssignmentStatus = "Available";
                }

                return order;
            }

            return null;
        }

        public async Task<List<LabOrderDto>> GetCompletedLabOrdersAsync(int labTechId)
        {
            var orders = new List<LabOrderDto>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT lo.*, 
                       d.FullName as DoctorName,
                       p.FullName as PatientName,
                       lt.FullName as LabTechName
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
                WHERE lo.LabTechId = @LabTechId 
                    AND lo.Status IN ('Completed', 'Reported')
                    AND lo.CompletedAt IS NOT NULL
                ORDER BY lo.CompletedAt DESC";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@LabTechId", labTechId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var order = MapLabOrderFromReader(reader);
                order.AssignmentStatus = "Assigned"; // All completed orders were assigned to this tech
                orders.Add(order);
            }

            return orders;
        }

        public async Task<bool> SubmitLabReportAsync(int orderId, int labTechId, SubmitLabReportDto reportDto)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // First, get the doctor ID for notification and verify the order exists
                var getDoctorQuery = @"
            SELECT DoctorId 
            FROM LabOrders 
            WHERE LabOrderId = @OrderId 
                AND (LabTechId = @LabTechId OR LabTechId IS NULL)
                AND Status IN ('Pending', 'InProgress')";

                int doctorId;

                using (var cmd = new SqlCommand(getDoctorQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@LabTechId", labTechId);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        transaction.Rollback();
                        return false; // Order not found or not accessible
                    }

                    doctorId = (int)result;
                }

                // Handle file upload if provided
                string? filePath = null;
                if (reportDto.ReportFile != null && reportDto.ReportFile.Length > 0)
                {
                    filePath = await SaveReportFileAsync(reportDto.ReportFile, orderId);
                }
                else if (!string.IsNullOrEmpty(reportDto.ReportFilePath))
                {
                    filePath = reportDto.ReportFilePath;
                }

                // Update the lab order with results and claim it if not already assigned
                var updateQuery = @"
            UPDATE LabOrders 
            SET Results = @Results,
                ReportFilePath = @ReportFilePath,
                Status = 'Completed',
                CompletedAt = GETDATE(),
                LabTechId = @LabTechId
            WHERE LabOrderId = @OrderId 
                AND (LabTechId = @LabTechId OR LabTechId IS NULL)
                AND Status IN ('Pending', 'InProgress')";

                using (var cmd = new SqlCommand(updateQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Results", reportDto.Results ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReportFilePath", filePath ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    cmd.Parameters.AddWithValue("@LabTechId", labTechId);

                    var rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                // Create notification for the doctor
                var notificationQuery = @"
            INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt)
            VALUES (@UserId, @Title, @Message, 0, GETDATE())";

                using (var cmd = new SqlCommand(notificationQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", doctorId);
                    cmd.Parameters.AddWithValue("@Title", "Lab Report Completed");
                    cmd.Parameters.AddWithValue("@Message", $"Lab report for Order #{orderId} is ready. Click to download.");

                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // Log the exception
                Console.WriteLine($"Error submitting lab report: {ex.Message}");
                throw;
            }
        }
        private void EnsureUploadDirectoryExists()
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "reports");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }
        }

        public async Task<bool> UpdateLabOrderStatusAsync(int orderId, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE LabOrders 
                SET Status = @Status,
                    CompletedAt = CASE WHEN @Status IN ('Completed', 'Reported') THEN GETDATE() ELSE CompletedAt END
                WHERE LabOrderId = @OrderId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<LabTechProfileDto?> GetLabTechProfileAsync(int labTechId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT lt.LabTechId, lt.Qualifications,
                       u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
                FROM LabTechnicianProfiles lt
                INNER JOIN Users u ON lt.LabTechId = u.UserId
                WHERE lt.LabTechId = @LabTechId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@LabTechId", labTechId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new LabTechProfileDto
                {
                    LabTechId = reader.GetInt32("LabTechId"),
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

        public async Task<LabTechProfileDto?> GetProfileByUserIdAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT lt.LabTechId, lt.Qualifications,
                       u.UserName, u.CNIC, u.PhoneNumber, u.FullName, u.ProfilePictureUrl
                FROM LabTechnicianProfiles lt
                INNER JOIN Users u ON lt.LabTechId = u.UserId
                WHERE u.UserId = @UserId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new LabTechProfileDto
                {
                    LabTechId = reader.GetInt32("LabTechId"),
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

        public async Task<bool> UpdateLabTechnicianProfileAsync(EditLabTechProfileDto editDto)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Handle profile picture as Base64 if provided
                string? profilePictureUrl = editDto.ProfilePictureUrl;

                // Update Users table
                var updateUserQuery = @"
            UPDATE Users 
            SET FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                ProfilePictureUrl = @ProfilePictureUrl
            WHERE UserId = (SELECT LabTechId FROM LabTechnicianProfiles WHERE LabTechId = @LabTechId)";

                using (var cmd = new SqlCommand(updateUserQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@FullName", editDto.FullName);
                    cmd.Parameters.AddWithValue("@PhoneNumber", editDto.PhoneNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfilePictureUrl", profilePictureUrl ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@LabTechId", editDto.LabTechId);

                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error updating lab technician profile: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SendNotificationToDoctorAsync(int doctorId, string message, int orderId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt)
                VALUES (@UserId, @Title, @Message, 0, GETDATE())";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", doctorId);
            cmd.Parameters.AddWithValue("@Title", "Lab Report Update");
            cmd.Parameters.AddWithValue("@Message", message);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> GetLabTechIdFromUserIdAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT LabTechId FROM LabTechnicianProfiles WHERE LabTechId = @UserId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (int)result : 0;
        }
       public async Task<bool> ChangeLabTechnicianPasswordAsync(EditLabTechPasswordDto passwordDto)
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // First verify the current password
        var verifyQuery = "SELECT PasswordHash FROM Users WHERE UserId = @LabTechId";
        using var verifyCommand = new SqlCommand(verifyQuery, connection);
        verifyCommand.Parameters.AddWithValue("@LabTechId", passwordDto.LabTechId);

        var currentHashObj = await verifyCommand.ExecuteScalarAsync();
        if (currentHashObj == null)
        {
            throw new InvalidOperationException("Lab technician not found.");
        }

        var currentHash = currentHashObj.ToString();
        if (!VerifyPassword(passwordDto.CurrentPassword, currentHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // Hash the new password
        var newPasswordHash = HashPassword(passwordDto.NewPassword);

        // Update the password
        var updateQuery = @"
            UPDATE Users 
            SET 
                PasswordHash = @NewPasswordHash,
                UpdatedAt = GETDATE()
            WHERE UserId = @LabTechId";

        using var updateCommand = new SqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@LabTechId", passwordDto.LabTechId);
        updateCommand.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("Failed to update password.");
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error changing lab technician password: {ex.Message}");
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
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}
        public async Task<List<LabOrderDto>> GetAvailableLabOrdersAsync(int labTechId)
        {
            var orders = new List<LabOrderDto>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        SELECT lo.*, 
               d.FullName as DoctorName,
               p.FullName as PatientName,
               lt.FullName as LabTechName
        FROM LabOrders lo
        INNER JOIN Users d ON lo.DoctorId = d.UserId
        INNER JOIN Users p ON lo.PatientId = p.UserId
        LEFT JOIN Users lt ON lo.LabTechId = lt.UserId
        WHERE lo.Status IN ('Pending', 'InProgress') 
            AND (lo.LabTechId IS NULL OR lo.LabTechId = @LabTechId)
        ORDER BY 
            CASE WHEN lo.LabTechId = @LabTechId THEN 0 ELSE 1 END, -- Assigned orders first
            lo.OrderDate ASC"; // Oldest orders first

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@LabTechId", labTechId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var order = MapLabOrderFromReader(reader);

                // Set assignment status
                if (order.LabTechId == labTechId)
                {
                    order.AssignmentStatus = "Assigned";
                }
                else if (order.LabTechId == null)
                {
                    order.AssignmentStatus = "Available";
                }
                else
                {
                    order.AssignmentStatus = "Other";
                }

                orders.Add(order);
            }

            return orders;
        }
        public async Task<string> SaveReportFileAsync(IFormFile file, int orderId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required and cannot be empty");

            // Validate file type (optional - you can add more restrictions)
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

            // Ensure directory exists
            EnsureUploadDirectoryExists();

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "reports");
            var fileName = $"report_{orderId}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                // Return the relative path for storage in database
                return $"uploads/reports/{fileName}";
            }
            catch (Exception ex)
            {
                // Clean up partial file if it exists
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { /* Ignore cleanup errors */ }
                }

                throw new InvalidOperationException($"Failed to save report file: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReportFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        private async Task<string> SaveProfilePictureAsync(IFormFile file, int labTechId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"labtech_{labTechId}_{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/profiles/{fileName}";
        }

        private LabOrderDto MapLabOrderFromReader(SqlDataReader reader)
        {
            return new LabOrderDto
            {
                LabOrderId = reader.GetInt32("LabOrderId"),
                AppointmentId = reader.IsDBNull("AppointmentId") ? null : reader.GetInt32("AppointmentId"),
                DoctorId = reader.GetInt32("DoctorId"),
                PatientId = reader.GetInt32("PatientId"),
                LabTechId = reader.IsDBNull("LabTechId") ? null : reader.GetInt32("LabTechId"),
                OrderDate = reader.GetDateTime("OrderDate"),
                Status = reader.GetString("Status"),
                Results = reader.IsDBNull("Results") ? null : reader.GetString("Results"),
                ReportFilePath = reader.IsDBNull("ReportFilePath") ? null : reader.GetString("ReportFilePath"),
                CompletedAt = reader.IsDBNull("CompletedAt") ? null : reader.GetDateTime("CompletedAt"),
                DoctorName = reader.GetString("DoctorName"),
                PatientName = reader.GetString("PatientName"),
                LabTechName = reader.IsDBNull("LabTechName") ? null : reader.GetString("LabTechName"),
                AssignmentStatus = string.Empty // This will be set by the calling method
            };
        }
        
    }
}