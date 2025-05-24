using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BlazorApp1.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IConfiguration configuration, 
            IWebHostEnvironment environment, 
            ILogger<ReportsController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Downloads a generated report by ID
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>PDF file stream</returns>
        [HttpGet("download/{id:int}")]
        public async Task<IActionResult> DownloadReport(int id)
        {
            try
            {
                // Get current user's ID from claims (assuming you have authentication)
                var currentUserId = GetCurrentUserId();
                
                // Look up the report in the database
                var report = await GetReportByIdAsync(id, currentUserId);
                
                if (report == null)
                {
                    _logger.LogWarning("Report with ID {ReportId} not found or access denied for user {UserId}", id, currentUserId);
                    return NotFound(new { message = "Report not found or access denied." });
                }

                // Map the web path to physical file path
                var webPath = report.FilePath;
                var fileName = Path.GetFileName(webPath);
                var physicalPath = Path.Combine(_environment.WebRootPath, "reports", fileName);

                // Check if file exists on disk
                if (!System.IO.File.Exists(physicalPath))
                {
                    _logger.LogError("Physical file not found: {PhysicalPath} for report ID {ReportId}", physicalPath, id);
                    return NotFound(new { message = "Report file not found on server." });
                }

                // Get file info
                var fileInfo = new FileInfo(physicalPath);
                
                // Log the download activity
                await LogDownloadActivityAsync(currentUserId, id, report.ReportType);

                // Return the file with appropriate headers
                var contentType = "application/pdf";
                var downloadFileName = $"{report.ReportType}_Report_{report.CreatedAt:yyyyMMdd_HHmmss}.pdf";

                return PhysicalFile(
                    physicalPath: physicalPath,
                    contentType: contentType,
                    fileDownloadName: downloadFileName,
                    enableRangeProcessing: true // Enables partial downloads for large files
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report with ID {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while downloading the report." });
            }
        }

        /// <summary>
        /// Gets a list of available reports for the current user
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of reports</returns>
        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var offset = (page - 1) * pageSize;

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // Get total count
                var countSql = @"
                    SELECT COUNT(*) 
                    FROM dbo.Reports 
                    WHERE AdminId = @UserId";

                int totalCount;
                using (var countCmd = new SqlCommand(countSql, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", currentUserId);
                    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                // Get paginated reports
                var reportsSql = @"
                    SELECT 
                        ReportId, ReportType, Parameters, FilePath, CreatedAt
                    FROM dbo.Reports 
                    WHERE AdminId = @UserId
                    ORDER BY CreatedAt DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var reports = new List<object>();
                using (var reportsCmd = new SqlCommand(reportsSql, conn))
                {
                    reportsCmd.Parameters.AddWithValue("@UserId", currentUserId);
                    reportsCmd.Parameters.AddWithValue("@Offset", offset);
                    reportsCmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using var reader = await reportsCmd.ExecuteReaderAsync();
   while (await reader.ReadAsync())
{
    reports.Add(new
    {
        ReportId = reader.GetInt32(reader.GetOrdinal("ReportId")),
        ReportType = reader.GetString(reader.GetOrdinal("ReportType")),
        Parameters = reader.GetString(reader.GetOrdinal("Parameters")),
        FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        // Ensure Url is defined and accessible in the scope where this code resides
        DownloadUrl = Url.Action(nameof(DownloadReport), new { id = reader.GetInt32(reader.GetOrdinal("ReportId")) })
    });
}

                }

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    Reports = reports,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving reports." });
            }
        }

        /// <summary>
        /// Deletes a report and its associated file
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>Success/failure result</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // Get the report to check ownership and get file path
                var report = await GetReportByIdAsync(id, currentUserId);
                
                if (report == null)
                {
                    return NotFound(new { message = "Report not found or access denied." });
                }

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var transaction = conn.BeginTransaction();

                try
                {
                    // Delete from database
                    var deleteSql = @"
                        DELETE FROM dbo.Reports 
                        WHERE ReportId = @ReportId AND AdminId = @UserId";

                    using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@ReportId", id);
                    deleteCmd.Parameters.AddWithValue("@UserId", currentUserId);

                    var rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                    
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return NotFound(new { message = "Report not found or access denied." });
                    }

                    // Log deletion activity
                    var logSql = @"
                        INSERT INTO dbo.ActivityLogs (AdminId, Action, Details, CreatedAt)
                        VALUES (@AdminId, @Action, @Details, SYSUTCDATETIME())";

                    using var logCmd = new SqlCommand(logSql, conn, transaction);
                    logCmd.Parameters.AddWithValue("@AdminId", currentUserId);
                    logCmd.Parameters.AddWithValue("@Action", "Delete Report");
                    logCmd.Parameters.AddWithValue("@Details", $"Deleted {report.ReportType} report with ID {id}");
                    await logCmd.ExecuteNonQueryAsync();

                    transaction.Commit();

                    // Try to delete the physical file (but don't fail if it doesn't exist)
                    var fileName = Path.GetFileName(report.FilePath);
                    var physicalPath = Path.Combine(_environment.WebRootPath, "reports", fileName);
                    
                    if (System.IO.File.Exists(physicalPath))
                    {
                        try
                        {
                            System.IO.File.Delete(physicalPath);
                            _logger.LogInformation("Deleted report file: {FilePath}", physicalPath);
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogWarning(fileEx, "Failed to delete report file: {FilePath}", physicalPath);
                            // Don't fail the operation if file deletion fails
                        }
                    }

                    return Ok(new { message = "Report deleted successfully." });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report with ID {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the report." });
            }
        }

        private async Task<ReportInfo?> GetReportByIdAsync(int reportId, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT ReportId, AdminId, ReportType, FilePath, CreatedAt
                FROM dbo.Reports 
                WHERE ReportId = @ReportId AND AdminId = @UserId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ReportId", reportId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            
if (await reader.ReadAsync())
{
    return new ReportInfo
    {
        ReportId = reader.GetInt32(reader.GetOrdinal("ReportId")),
        AdminId = reader.GetInt32(reader.GetOrdinal("AdminId")),
        ReportType = reader.GetString(reader.GetOrdinal("ReportType")),
        FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
    };
}

            return null;
        }

        private async Task LogDownloadActivityAsync(int userId, int reportId, string reportType)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO dbo.ActivityLogs (AdminId, Action, Details, CreatedAt)
                    VALUES (@AdminId, @Action, @Details, SYSUTCDATETIME())";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AdminId", userId);
                cmd.Parameters.AddWithValue("@Action", "Download Report");
                cmd.Parameters.AddWithValue("@Details", $"Downloaded {reportType} report with ID {reportId}");
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log download activity for report {ReportId}", reportId);
                // Don't throw - logging failure shouldn't prevent download
            }
        }

        private int GetCurrentUserId()
        {
            // This assumes you have authentication set up and the user ID is in claims
            // Adjust this based on your authentication implementation
  

            return 13;
        }

        private class ReportInfo
        {
            public int ReportId { get; set; }
            public int AdminId { get; set; }
            public string ReportType { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}