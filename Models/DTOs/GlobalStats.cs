namespace BlazorApp1.Models.DTOs
{
    public class GlobalStatsDto
    {
        public int TotalAdmins { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalReceptionists { get; set; }
        public int TotalPatients { get; set; }
        public int TotalLabTechnicians { get; set; }
        
        // Appointment statistics
        public int PendingAppointments { get; set; }
        public int ApprovedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        
        // Lab order statistics
        public int PendingLabOrders { get; set; }
        public int InProgressLabOrders { get; set; }
        public int CompletedLabOrders { get; set; }
        public int CancelledLabOrders { get; set; }
    }
}