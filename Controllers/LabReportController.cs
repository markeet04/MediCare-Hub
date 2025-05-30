using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlazorApp1.Services;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace BlazorApp1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LabReportsController : ControllerBase
    {
        private readonly ILabTechnicianService _labTechService;
        private readonly IWebHostEnvironment _environment;
        private readonly string _connectionString;

        public LabReportsController(
            ILabTechnicianService labTechService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _labTechService = labTechService;
            _environment = environment;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string not found");
        }

        [HttpGet("laborder/download/{orderId:int}")]
        public async Task<IActionResult> DownloadLabReport(int orderId)
        {
            try
            {
                // Get current user information
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Unauthorized("Unable to determine user identity.");
                }

                // Get the lab order and verify access permissions
                var labOrder = await GetLabOrderWithPermissionCheck(orderId, currentUserId, userRole);

                if (labOrder == null)
                {
                    return NotFound("Lab order not found or you don't have permission to access it.");
                }

                if (string.IsNullOrEmpty(labOrder.ReportFilePath))
                {
                    return NotFound("No report file is available for this order.");
                }

                // Construct the full file path
                var filePath = Path.Combine(_environment.WebRootPath, labOrder.ReportFilePath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Report file not found on server.");
                }

                // Determine content type based on file extension
                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                var contentType = fileExtension switch
                {
                    ".pdf" => "application/pdf",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };

                // Generate a user-friendly filename
                var fileName = $"LabReport_Order{orderId}_{DateTime.Now:yyyyMMdd}{fileExtension}";

                // Return the file
                return PhysicalFile(filePath, contentType, fileName);
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a proper logging framework)
                Console.WriteLine($"Error downloading lab report: {ex.Message}");
                return StatusCode(500, "An error occurred while downloading the report.");
            }
        }

        [HttpGet("laborder/info/{orderId:int}")]
        public async Task<IActionResult> GetLabOrderInfo(int orderId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Unauthorized();
                }

                var labOrder = await GetLabOrderWithPermissionCheck(orderId, currentUserId, userRole);

                if (labOrder == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    OrderId = labOrder.LabOrderId,
                    PatientName = labOrder.PatientName,
                    DoctorName = labOrder.DoctorName,
                    OrderDate = labOrder.OrderDate,
                    Status = labOrder.Status,
                    Results = labOrder.Results,
                    HasReportFile = !string.IsNullOrEmpty(labOrder.ReportFilePath),
                    CompletedAt = labOrder.CompletedAt
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting lab order info: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving order information.");
            }
        }

        private async Task<LabOrderInfo?> GetLabOrderWithPermissionCheck(int orderId, int currentUserId, string? userRole)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT lo.LabOrderId, lo.DoctorId, lo.PatientId, lo.LabTechId, lo.OrderDate, 
                       lo.Status, lo.Results, lo.ReportFilePath, lo.CompletedAt,
                       d.FullName as DoctorName, p.FullName as PatientName
                FROM LabOrders lo
                INNER JOIN Users d ON lo.DoctorId = d.UserId
                INNER JOIN Users p ON lo.PatientId = p.UserId
                WHERE lo.LabOrderId = @OrderId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await cmd.ExecuteReaderAsync();
            LabOrderInfo? labOrder = null;
            if (await reader.ReadAsync())
            {
                labOrder = new LabOrderInfo
                {
                    LabOrderId = reader.GetInt32(reader.GetOrdinal("LabOrderId")),
                    DoctorId = reader.GetInt32(reader.GetOrdinal("DoctorId")),
                    PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                    LabTechId = reader.IsDBNull(reader.GetOrdinal("LabTechId")) ? null : reader.GetInt32(reader.GetOrdinal("LabTechId")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Results = reader.IsDBNull(reader.GetOrdinal("Results")) ? null : reader.GetString(reader.GetOrdinal("Results")),
                    ReportFilePath = reader.IsDBNull(reader.GetOrdinal("ReportFilePath")) ? null : reader.GetString(reader.GetOrdinal("ReportFilePath")),
                    CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                    DoctorName = reader.GetString(reader.GetOrdinal("DoctorName")),
                    PatientName = reader.GetString(reader.GetOrdinal("PatientName"))
                };
            }

            // Check permissions based on user role
            if (labOrder != null)
            {
                bool hasAccess = userRole switch
                {
                    "Doctor" => labOrder.DoctorId == currentUserId,
                    "LabTechnician" => labOrder.LabTechId == currentUserId || labOrder.LabTechId == null,
                    "Admin" => true,
                    _ => false
                };

                return hasAccess ? labOrder : null;
            }

            return null;
        }

        // Helper class for internal use
        public class LabOrderInfo
        {
            public int LabOrderId { get; set; }
            public int DoctorId { get; set; }
            public int PatientId { get; set; }
            public int? LabTechId { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Results { get; set; }
            public string? ReportFilePath { get; set; }
            public DateTime? CompletedAt { get; set; }
            public string DoctorName { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
        }
    }
}
