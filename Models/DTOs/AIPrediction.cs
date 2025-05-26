namespace BlazorApp1.Models.DTOs
{
    public class AIPredictionDto
    {
        public int PredictionId { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string PredictionType { get; set; } = string.Empty; // Risk, Treatment
        public string Data { get; set; } = string.Empty; // JSON string
        public DateTime CreatedAt { get; set; }
        // Navigation properties
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
    }
}