// Add these DTOs to your Models/DTOs folder

namespace BlazorApp1.Models.DTOs
{
    public class EditLabTechProfileDto
    {
        public int LabTechId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }

    public class EditLabTechPasswordDto
    {
        public int LabTechId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}