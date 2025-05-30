using BlazorApp1.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace BlazorApp1.Services
{
    public interface ILabTechnicianService
    {
        // Dashboard and stats
        Task<LabTechnicianDashboardStatsDto> GetDashboardStatsAsync(int labTechId);
        
        // Order management - Updated methods
        Task<List<LabOrderDto>> GetAvailableAndAssignedOrdersAsync(int labTechId);
        Task<LabOrderDto?> GetLabOrderByIdAsync(int orderId);
        Task<List<LabOrderDto>> GetCompletedLabOrdersAsync(int labTechId);
        Task<bool> ClaimLabOrderAsync(int orderId, int labTechId);

        // Report submission
        Task<bool> SubmitLabReportAsync(int orderId, int labTechId, SubmitLabReportDto reportDto);       
 Task<bool> UpdateLabOrderStatusAsync(int orderId, string status);
        
        // Profile management
        Task<LabTechProfileDto?> GetLabTechProfileAsync(int labTechId);
        Task<LabTechProfileDto?> GetProfileByUserIdAsync(int userId);
         Task<bool> UpdateLabTechnicianProfileAsync(EditLabTechProfileDto editDto);
    Task<bool> ChangeLabTechnicianPasswordAsync(EditLabTechPasswordDto passwordDto);
        // Utility methods
        Task<int> GetLabTechIdFromUserIdAsync(int userId);
        Task<bool> SendNotificationToDoctorAsync(int doctorId, string message, int orderId);
        Task<List<LabOrderDto>> GetAvailableLabOrdersAsync(int labTechId);

        // File handling
        Task<string> SaveReportFileAsync(IFormFile file, int orderId);
        Task<bool> DeleteReportFileAsync(string filePath);
    }
}