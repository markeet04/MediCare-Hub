using System;

namespace BlazorApp1.Models.DTOs
{
    public class CreateAppointmentDto
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public string AdditionalNotes { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}
