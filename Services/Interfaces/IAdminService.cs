using BlazorApp1.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Services.Interfaces
{
    public interface IAdminService
    {Task LogActivityAsync(int adminId, string action, string details);

        Task<(int userId, string tempPassword)> CreateDoctorAsync(CreateDoctorDto dto);
        Task<(int userId, string tempPassword)> CreateReceptionistAsync(CreateReceptionistDto dto);
        Task<(int userId, string tempPassword)> CreatePatientAsync(CreatePatientDto dto);
        Task<(int userId, string tempPassword)> CreateLabTechAsync(CreateLabTechDto dto);
        Task UpdateAdminProfileAsync(EditAdminProfileDto dto);
        Task<EditAdminProfileDto> GetAdminByIdAsync(int adminId);
        
        Task<GlobalStatsDto> GetGlobalStatsAsync();
        Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync();
        Task<int> GenerateReportAsync(GenerateReportRequestDto dto);
        Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int adminId);
        Task<LabTechProfileDto> GetLabTechByIdAsync(int labTechId);
        Task<ReceptionistProfileDto> GetReceptionistByIdAsync(int receptionistId);
        Task<PatientProfileDto> GetPatientByIdAsync(int patientId);
        Task<DoctorProfileDto> GetDoctorByIdAsync(int doctorId);
        // Add these to your IAdminService.cs interface:

        // Update methods
        Task UpdateDoctorAsync(UpdateDoctorDto dto);
Task UpdateReceptionistAsync(UpdateReceptionistDto dto);
Task UpdatePatientAsync(UpdatePatientDto dto);
Task UpdateLabTechAsync(UpdateLabTechDto dto);

// Delete methods
Task DeleteDoctorAsync(int doctorId);
Task DeleteReceptionistAsync(int receptionistId);
Task DeletePatientAsync(int patientId);
Task DeleteLabTechAsync(int labTechId);

// Get all methods for listing users
Task<IEnumerable<DoctorProfileDto>> GetAllDoctorsAsync();
Task<IEnumerable<ReceptionistProfileDto>> GetAllReceptionistsAsync();
Task<IEnumerable<PatientProfileDto>> GetAllPatientsAsync();
Task<IEnumerable<LabTechProfileDto>> GetAllLabTechsAsync();
    }
}