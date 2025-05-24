using System;

namespace BlazorApp1.Models.DTOs
{
    public class ActivityLogDto
    {
        public int LogId { get; set; }
        public int AdminId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation property
        public string AdminName { get; set; } = string.Empty;
    }
}