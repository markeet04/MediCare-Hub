namespace BlazorApp1.Models.DTOs
{
    public class PatientSummaryDto
    {
        public int PatientId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string CNIC { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? BloodGroup { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalAppointments { get; set; }
        public DateTime? LastAppointmentDate { get; set; }
        public int? Age => DateOfBirth.HasValue ?
            DateTime.Now.Year - DateOfBirth.Value.Year -
            (DateTime.Now.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0) : null;
    }
}