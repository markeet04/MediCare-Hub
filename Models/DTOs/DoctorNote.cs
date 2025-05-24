using System;

namespace BlazorApp1.Models.DTOs
{
    public class DoctorNoteDto
    {
        public int NoteId { get; set; }
        public int? AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
    }
}