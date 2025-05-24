using System;

namespace BlazorApp1.Models.DTOs
{
    public class LabOrderDto
    {
        public int LabOrderId { get; set; }
        public int? AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public int? LabTechId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Results { get; set; }
        public string? ReportFilePath { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? LabTechName { get; set; }
    }
}