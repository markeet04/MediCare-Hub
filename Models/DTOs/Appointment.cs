using System;

namespace BlazorApp1.Models.DTOs
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? ReceptionistId { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public string PatientName { get; set; } = string.Empty;
        public decimal? ConsultationFee { get; set; }

        public string DoctorName { get; set; } = string.Empty;
        public string? ReceptionistName { get; set; }
        public string? PatientPhone { get; set; }
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }
        public string Department { get; set; } = string.Empty;
    }
}