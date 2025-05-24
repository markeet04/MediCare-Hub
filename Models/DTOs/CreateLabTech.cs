namespace BlazorApp1.Models.DTOs
{
    public class CreateLabTechDto
    {
        public string UserName { get; set; } = string.Empty;
        public string CNIC { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Qualifications { get; set; }
    }
}