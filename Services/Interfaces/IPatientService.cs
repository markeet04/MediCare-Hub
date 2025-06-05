using BlazorApp1.Models.DTOs;

namespace BlazorApp1.Services.Interfaces
{
    public interface IPatientService
    {
        // Existing methods
        Task<(int userId, string tempPassword)> CreatePatientAsync(CreatePatientDto dto, string password);
        Task<PatientProfileDto?> GetPatientByUserIdAsync(int userId);
        
        Task<PatientHistoryDto?> GetLatestHistoryAsync(int patientId);
        Task<List<PatientHistoryDto>> GetAllPatientHistoriesAsync(int patientId);
        Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync(int patientId);
        Task<List<NotificationDto>> GetRecentNotificationsAsync(int patientId);
        Task<int> GetUnreadCountAsync(int patientId);
        Task MarkAllNotificationsReadAsync(int patientId);

        // Appointment and notification management
        Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId);
        Task<bool> CancelAppointmentAsync(int appointmentId, int patientId, string reason = "Cancelled by patient");
        Task<bool> SendNotificationAsync(int userId, string title, string message);
        Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId);
        Task<bool> BookAppointmentAsync(CreateAppointmentDto appointmentDto);
        Task<List<DoctorProfileDto>> GetAllDoctorsAsync();
        Task<List<string>> GetDoctorSpecialtiesAsync();
        Task<bool> IsDoctorSlotTakenAsync(int doctorId, DateTime appointmentDateTime);
        
        // Lab order methods
        Task<List<LabOrderDto>> GetPatientLabOrdersAsync(int patientId);
        Task<List<LabOrderDto>> GetSubmittedLabOrdersAsync(int patientId);
        Task<LabOrderDto?> GetLabOrderByIdAsync(int labOrderId);
        Task<List<LabOrderDto>> GetLabOrdersByStatusAsync(int patientId, string status);

        // New profile management methods
        Task<bool> UpdatePatientProfileAsync(int patientId, UpdatePatientProfileRequest request);
        Task<bool> ChangePasswordAsync(ChangePatientPasswordRequest request);
    }
}