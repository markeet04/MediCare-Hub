namespace BlazorApp1.Models.DTOs
{
    public class PatientHistoryDto
    {
        public int HistoryId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime EncounterDate { get; set; }

        // Free-text fields
        public string? Symptoms { get; set; }
        public string? Notes { get; set; }

        // Vitals
        public decimal? WeightKg { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? BMI { get; set; }
        public int? PulseBPM { get; set; }
        public string? BloodPressure { get; set; }
        public decimal? TemperatureC { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? OxygenSatPct { get; set; }
    }
}
