namespace BlazorApp1.Models.DTOs
{
    public class EditAdminProfileDto
    {
        public int AdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? NewPassword { get; set; }
        public string? OfficeLocation { get; set; }
    }
}