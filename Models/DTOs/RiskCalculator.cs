namespace BlazorApp1.Models.DTOs
{
    public class QriskRequestDto
    {
        public int Age { get; set; }
        public bool IsMale { get; set; }
        public string? Ethnicity { get; set; }
        public string? Postcode { get; set; } // For Townsend deprivation score
        public bool SmokingStatus { get; set; }
        public bool DiabetesType1 { get; set; }
        public bool DiabetesType2 { get; set; }
        public bool FamilyHistoryCVD { get; set; }
        public bool ChronicKidneyDisease { get; set; }
        public bool AtrialFibrillation { get; set; }
        public bool BloodPressureTreatment { get; set; }
        public bool RheumatoidArthritis { get; set; }
        public decimal SystolicBP { get; set; }
        public decimal CholesterolHDLRatio { get; set; }
        public decimal BMI { get; set; }
    }

    public class FindriscRequestDto
    {
        public int Age { get; set; }
        public decimal BMI { get; set; }
        public decimal WaistCircumference { get; set; }
        public bool DailyPhysicalActivity { get; set; }
        public bool EatsVegetablesDaily { get; set; }
        public bool TakesBloodPressureMedication { get; set; }
        public bool HistoryHighBloodGlucose { get; set; }
        public bool FamilyHistoryDiabetes { get; set; }
    }

    public class Curb65RequestDto
    {
        public int Age { get; set; }
        public bool Confusion { get; set; }
        public decimal Urea { get; set; } // mmol/L
        public int RespiratoryRate { get; set; }
        public decimal SystolicBP { get; set; }
        public decimal DiastolicBP { get; set; }
    }
        public class QriskResponseDto
    {
        public decimal CVDRiskPercentage { get; set; }
        public string RiskCategory { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
    }

    public class FindriscResponseDto
    {
        public int Points { get; set; }
        public decimal DiabetesRiskPercentage { get; set; }
        public string RiskCategory { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
    }

    public class Curb65ResponseDto
    {
        public int Score { get; set; }
        public decimal MortalityRiskPercentage { get; set; }
        public string RiskCategory { get; set; } = string.Empty;
        public string RecommendedCare { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
    }
}