namespace BlazorApp1.Models.DTOs
{
    public class ReceptionistProfileDto
    {
        public int ReceptionistId { get; set; }
        
        // User information
        public string UserName { get; set; } = string.Empty;
        public string CNIC { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}