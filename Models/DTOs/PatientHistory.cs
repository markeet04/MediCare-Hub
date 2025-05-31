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
        // QRisk-3 relevant fields
public string? Ethnicity { get; set; }
public string? SmokingStatus { get; set; }
public decimal? TotalCholesterol { get; set; }
public decimal? HDLCholesterol { get; set; }
public string? DiabetesType { get; set; }
public bool? FamilyCVDHistory { get; set; }
public bool? OnBPMedication { get; set; }
public bool? OnStatin { get; set; }

// FINDRISC fields
public decimal? WaistCm { get; set; }
public bool? PhysicalActivity { get; set; }
public bool? EatsVegetablesDaily { get; set; }
public bool? HighBloodGlucoseHx { get; set; }
public bool? FamilyDiabetesHistory { get; set; }

// CURB-65 fields
public bool? Confusion { get; set; }
public decimal? BloodUreaMmolPerL { get; set; }
    }
}
