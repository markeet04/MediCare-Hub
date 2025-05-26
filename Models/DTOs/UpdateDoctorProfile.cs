namespace BlazorApp1.Models.DTOs
{
public class UpdateDoctorProfileRequest
    {
     
        public string FullName { get; set; } = string.Empty;

       
        public string? PhoneNumber { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
}