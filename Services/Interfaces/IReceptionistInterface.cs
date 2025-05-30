using BlazorApp1.Models.DTOs;

namespace BlazorApp1.Services.Interfaces
{
    public interface IReceptionistService
    {
        // Dashboard Statistics
        Task<int> GetTodayAppointmentsCountAsync();
        Task<int> GetPendingAppointmentsCountAsync();
        Task<int> GetTotalPatientsCountAsync();
        Task<List<AppointmentDto>> GetRecentAppointmentsAsync(int count = 10);
        Task<List<NotificationDto>> GetReceptionistNotificationsAsync(int receptionistId, int count = 5);

        // Walk-in Registration
        Task<int> RegisterWalkInPatientAsync(PatientProfileDto patientProfile, string password);
        Task<bool> CheckCNICExistsAsync(string cnic);
        Task<bool> CheckUserNameExistsAsync(string userName);

        // Appointment Management
        Task<List<AppointmentDto>> GetAllAppointmentsAsync(int pageNumber = 1, int pageSize = 20);
        Task<List<AppointmentDto>> GetAppointmentsByDateAsync(DateTime date);
        Task<List<AppointmentDto>> GetAppointmentsByStatusAsync(string status);
        Task<List<AppointmentDto>> GetAppointmentsByPatientAsync(int patientId);
        Task<List<AppointmentDto>> GetAppointmentsByDoctorAsync(int doctorId);
        Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId);
        Task<bool> UpdateAppointmentStatusAsync(int appointmentId, string status, int receptionistId);
        Task<bool> RescheduleAppointmentAsync(int appointmentId, DateTime newDateTime, int receptionistId);
        Task<bool> AssignReceptionistToAppointmentAsync(int appointmentId, int receptionistId);
        Task<int> CreateAppointmentAsync(AppointmentDto appointment);
        Task<bool> CancelAppointmentAsync(int appointmentId, int receptionistId);

        // Patient Directory
        Task<List<PatientProfileDto>> GetAllPatientsAsync(int pageNumber = 1, int pageSize = 20);
        Task<List<PatientProfileDto>> SearchPatientsAsync(string searchTerm);
        Task<PatientProfileDto?> GetPatientByIdAsync(int patientId);
        Task<PatientProfileDto?> GetPatientByCNICAsync(string cnic);
        Task<List<AppointmentDto>> GetPatientAppointmentHistoryAsync(int patientId);
        Task<bool> UpdatePatientProfileAsync(PatientProfileDto patientProfile);

        // Doctor Information (for appointment scheduling)
        Task<List<DoctorProfileDto>> GetAllDoctorsAsync();
        Task<List<DoctorProfileDto>> GetDoctorsBySpecialtyAsync(string specialty);
        Task<DoctorProfileDto?> GetDoctorByIdAsync(int doctorId);

        // Profile Management
        Task<ReceptionistProfileDto?> GetReceptionistProfileAsync(int receptionistId);
        Task<bool> UpdateReceptionistProfileAsync(ReceptionistProfileDto profile);
Task ChangePasswordAsync(EditReceptionistPassword dto);
        // Utility Methods
        Task<List<string>> GetAvailableStatusesAsync();
        Task<List<string>> GetDoctorSpecialtiesAsync();
        Task<bool> CheckInPatientAsync(int appointmentId, int receptionistId);
Task SendNotificationToDoctorAsync(int doctorId, string title, string message);
Task<bool> IsDoctorSlotTakenAsync(int doctorId, DateTime scheduledDateTime);

        Task<bool> IsAppointmentTimeAvailableAsync(int doctorId, DateTime scheduledDateTime, int? excludeAppointmentId = null);
    }

    // Additional DTOs for Receptionist Service
    
}