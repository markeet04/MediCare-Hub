namespace BlazorApp1.Models.DTOs
{
    public class LabTechnicianDashboardStatsDto
    {
        public int PendingOrders { get; set; }
        public int CompletedToday { get; set; }
        public int TotalCompleted { get; set; }
        public int OverdueOrders { get; set; }
    }
    public class SubmitLabReportDto
    {
        public string? Results { get; set; }
        public string? ReportFilePath { get; set; }
        public IFormFile? ReportFile { get; set; }
        public string? Notes { get; set; }
    }
        public class UpdateLabTechProfileDto
    {
        public string? PhoneNumber { get; set; }
        public string? Qualifications { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
    }