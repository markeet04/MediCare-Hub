namespace BlazorApp1.Models.DTOs
{
       public class CreateDoctorNoteRequest
    {        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }
        
        public string NoteText { get; set; } = string.Empty;
    }
}