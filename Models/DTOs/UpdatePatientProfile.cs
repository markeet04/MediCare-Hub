using System;

namespace BlazorApp1.Models.DTOs
{
    public class UpdatePatientProfileRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = true;
        public bool TestResultsNotifications { get; set; } = true;
    }

    public class ChangePatientPasswordRequest
    {
        public int PatientId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}