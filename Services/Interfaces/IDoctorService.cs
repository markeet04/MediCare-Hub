using BlazorApp1.Models.DTOs;
namespace BlazorApp1.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<DoctorProfileDto?> GetDoctorProfileAsync(int doctorId);
        Task<DoctorProfileDto?> GetDoctorProfileByUserIdAsync(int userId);
        Task<IEnumerable<PatientSummaryDto>> GetAssignedPatientsAsync(int doctorId);
        Task<IEnumerable<AppointmentDto>> GetDoctorAppointmentsAsync(int doctorId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<AppointmentDto>> GetTodayAppointmentsAsync(int doctorId);
        Task<int> AddDoctorNoteAsync(int doctorId, CreateDoctorNoteRequest request);
        Task<IEnumerable<DoctorNoteDto>> GetDoctorNotesAsync(int doctorId, int? patientId = null);
        Task<int> CreateLabOrderAsync(int doctorId, CreateLabOrderRequest request);
        Task<IEnumerable<LabOrderDto>> GetDoctorLabOrdersAsync(int doctorId, string? status = null);
        Task<int> CreateAIPredictionAsync(int doctorId, AIPredictionRequest request);
        Task<IEnumerable<AIPredictionDto>> GetAIPredictionsAsync(int doctorId, int? patientId = null);
        Task UpdateDoctorProfileAsync(int doctorId, UpdateDoctorProfileRequest request);
        Task<PatientSummaryDto?> GetPatientSummaryAsync(int patientId);
        Task<int> AddPatientHistoryAsync(PatientHistoryDto dto);
        Task UpdatePatientHistoryAsync(PatientHistoryDto dto);
        Task<IEnumerable<PatientHistoryDto>> GetPatientHistoryAsync(int patientId);
Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int adminId);
    Task MarkNotificationAsReadAsync(int notificationId);
    Task MarkAllNotificationsAsReadAsync(int adminId);
        // Password management
        Task ChangePasswordAsync(ChangeDoctorPasswordRequest dto);
    }
}