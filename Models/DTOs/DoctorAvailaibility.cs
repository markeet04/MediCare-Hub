// File: Models/DTOs/DoctorAvailabilityDto.cs
namespace BlazorApp1.Models.DTOs
{
    public class DoctorAvailabilityDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public int AvailableSlots { get; set; }
    }
}
