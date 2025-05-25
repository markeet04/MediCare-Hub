using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Models.DTOs
{
    public class EditAdminPasswordDto
    {
        [Required]
        public int AdminId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}