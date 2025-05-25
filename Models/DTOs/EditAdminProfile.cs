using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Models.DTOs
{
    public class EditAdminProfileDto
    {
        [Required]
        public int AdminId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Url]
        [StringLength(255)]
        public string? ProfilePictureUrl { get; set; }

        [StringLength(100)]
        public string? OfficeLocation { get; set; }
    }
}