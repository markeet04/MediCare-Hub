using System;

namespace BlazorApp1.Models.DTOs
{
    public class GenerateReportRequestDto
    {
        public int AdminId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AdditionalFilters { get; set; }
    }
}