using System;

namespace BlazorApp1.Models.DTOs
{
    public class ReportDto
    {
        public int ReportId { get; set; }
        public int AdminId { get; set; }
        public string? ReportType { get; set; }
        public string? Parameters { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation property
        public string AdminName { get; set; } = string.Empty;
    }
}