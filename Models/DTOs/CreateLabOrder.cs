namespace BlazorApp1.Models.DTOs
{
 public class CreateLabOrderRequest
    {
        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }
       
        public string TestType { get; set; } = string.Empty;
        
        public string? Instructions { get; set; }
    }
}