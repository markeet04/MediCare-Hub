    namespace BlazorApp1.Models.DTOs
    {
        public class CreateDoctorDto
        {
            public string UserName { get; set; } = string.Empty;
            public string CNIC { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string? ProfilePictureUrl { get; set; }
            public string? Specialty { get; set; }
            public string? Qualifications { get; set; }
        }
    }