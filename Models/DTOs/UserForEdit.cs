using System;

namespace BlazorApp1.Models.DTOs
{
    public class UserForEditDto
    {
        public int UserId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        
        // Role-specific properties
        public string? Specialty { get; set; }          // for Doctor
        public string? Qualifications { get; set; }     // for Doctor or LabTech
        public DateTime? DateOfBirth { get; set; }      // for Patient
        public string? Gender { get; set; }             // for Patient
        public string? Address { get; set; }            // for Patient
        public string? BloodGroup { get; set; }         // for Patient
        // No extra properties for Receptionist
    }
}