using BlazorApp1.Helpers;
using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;

    public class QriskService : IQriskService
    {
        public async Task<QriskResponseDto> CalculateQriskAsync(QriskRequestDto request)
        {
            await Task.CompletedTask; // Make async for consistency
            
            var riskPercentage = Qrisk3Calculator.Calculate(
                request.Age, request.IsMale, request.Ethnicity ?? "White",
                0, // Default Townsend score - would need postcode lookup
                request.SmokingStatus, request.DiabetesType1, request.DiabetesType2,
                request.FamilyHistoryCVD, request.ChronicKidneyDisease, request.AtrialFibrillation,
                request.BloodPressureTreatment, request.RheumatoidArthritis,
                request.SystolicBP, request.CholesterolHDLRatio, request.BMI
            );

            return new QriskResponseDto
            {
                CVDRiskPercentage = riskPercentage,
                RiskCategory = Qrisk3Calculator.GetRiskCategory(riskPercentage),
                CalculatedAt = DateTime.UtcNow
            };
        }

      // QriskService.cs
public async Task<QriskResponseDto> CalculateFromPatientHistoryAsync(
    PatientHistoryDto patientHistory,
    PatientProfileDto patientProfile)
{
    // Calculate age (same as before)
    var age = patientProfile.DateOfBirth.HasValue
        ? DateTime.Today.Year - patientProfile.DateOfBirth.Value.Year
        : 50;

    // Map PatientHistoryDto → QriskRequestDto using new fields
    var request = new QriskRequestDto
    {
        Age = age,
        IsMale = patientProfile.Gender?.ToLower() == "male",

        // Use ethnicity if present; otherwise default to "White"
        Ethnicity = patientHistory.Ethnicity ?? "White",

        // We’re not doing Townsend score lookups yet, so keep 0

        // SmokingStatus: treat "current" as smoker; else false
        SmokingStatus = (patientHistory.SmokingStatus?.ToLower() == "current"),

        // DiabetesType1 / DiabetesType2 based on string value
        DiabetesType1 = (patientHistory.DiabetesType?.ToLower() == "type1"),
        DiabetesType2 = (patientHistory.DiabetesType?.ToLower() == "type2"),

        FamilyHistoryCVD = patientHistory.FamilyCVDHistory ?? false,

        // We don’t have a field for chronic kidney disease (yet), so leave false
        ChronicKidneyDisease = false,

        // We don’t have AtrialFibrillation in PatientHistoryDto, so leave false
        AtrialFibrillation = false,

        // OnBPMedication maps directly to "BP treatment" flag
        BloodPressureTreatment = patientHistory.OnBPMedication ?? false,

        // We don’t have rheumatoid arthritis in PatientHistoryDto, so leave false
        RheumatoidArthritis = false,

        // SystolicBP is parsed from the BloodPressure string
        SystolicBP = ExtractSystolicBP(patientHistory.BloodPressure),

        // Use real Cholesterol/HDL ratio if both values exist
        CholesterolHDLRatio =
            (patientHistory.TotalCholesterol.HasValue && patientHistory.HDLCholesterol.HasValue)
                ? patientHistory.TotalCholesterol.Value / patientHistory.HDLCholesterol.Value
                : 4.0m, // fallback if either is null

        // BMI from history (fallback to 25 if missing)
        BMI = patientHistory.BMI ?? 25.0m
    };

    return await CalculateQriskAsync(request);
}




        private decimal ExtractSystolicBP(string? bloodPressure)
        {
            if (string.IsNullOrEmpty(bloodPressure)) return 120m;
            
            var parts = bloodPressure.Split('/');
            if (parts.Length > 0 && decimal.TryParse(parts[0], out var systolic))
                return systolic;
            
            return 120m; // Default
        }
    }

    public class FindriscService : IFindriscService
    {
        public async Task<FindriscResponseDto> CalculateFindriscAsync(FindriscRequestDto request)
        {
            await Task.CompletedTask;
            
            var points = FindriscCalculator.CalculatePoints(
                request.Age, request.BMI, request.WaistCircumference,
                request.DailyPhysicalActivity, request.EatsVegetablesDaily,
                request.TakesBloodPressureMedication, request.HistoryHighBloodGlucose,
                request.FamilyHistoryDiabetes
            );

            var riskPercentage = FindriscCalculator.MapPointsToPercentage(points);

            return new FindriscResponseDto
            {
                Points = points,
                DiabetesRiskPercentage = riskPercentage,
                RiskCategory = FindriscCalculator.GetRiskCategory(points),
                CalculatedAt = DateTime.UtcNow
            };
        }

 // FindriscService.cs
public async Task<FindriscResponseDto> CalculateFromPatientHistoryAsync(
    PatientHistoryDto patientHistory,
    PatientProfileDto patientProfile)
{
    var age = patientProfile.DateOfBirth.HasValue
        ? DateTime.Today.Year - patientProfile.DateOfBirth.Value.Year
        : 50;

    var request = new FindriscRequestDto
    {
        Age = age,

        // BMI from history; fallback to 25
        BMI = patientHistory.BMI ?? 25.0m,

        // Waist circumference from history; fallback to 85
        WaistCircumference = patientHistory.WaistCm ?? 85.0m,

        // Daily physical activity flag from history; fallback to true
        DailyPhysicalActivity = patientHistory.PhysicalActivity ?? true,

        // Eats vegetables daily flag; fallback to true
        EatsVegetablesDaily = patientHistory.EatsVegetablesDaily ?? true,

        // Takes BP medication if OnBPMedication is true; else false
        TakesBloodPressureMedication = patientHistory.OnBPMedication ?? false,

        // History of high blood glucose from history; fallback to false
        HistoryHighBloodGlucose = patientHistory.HighBloodGlucoseHx ?? false,

        // Family history of diabetes from history; fallback to false
        FamilyHistoryDiabetes = patientHistory.FamilyDiabetesHistory ?? false
    };

    return await CalculateFindriscAsync(request);
}

    }

    public class Curb65Service : ICurb65Service
    {
        public async Task<Curb65ResponseDto> CalculateCurb65Async(Curb65RequestDto request)
        {
            await Task.CompletedTask;
            
            var score = Curb65Calculator.CalculateScore(
                request.Age, request.Confusion, request.Urea,
                request.RespiratoryRate, request.SystolicBP, request.DiastolicBP
            );

            var mortalityRisk = Curb65Calculator.GetMortalityRisk(score);
            var (riskCategory, recommendedCare) = Curb65Calculator.GetRiskAssessment(score);

            return new Curb65ResponseDto
            {
                Score = score,
                MortalityRiskPercentage = mortalityRisk,
                RiskCategory = riskCategory,
                RecommendedCare = recommendedCare,
                CalculatedAt = DateTime.UtcNow
            };
        }
// Curb65Service.cs
public async Task<Curb65ResponseDto> CalculateFromPatientHistoryAsync(
    PatientHistoryDto patientHistory,
    PatientProfileDto patientProfile)
{
    var age = patientProfile.DateOfBirth.HasValue
        ? DateTime.Today.Year - patientProfile.DateOfBirth.Value.Year
        : 50;

    // Parse systolic and diastolic from BloodPressure string
    var (systolic, diastolic) = ExtractBloodPressure(patientHistory.BloodPressure);

    var request = new Curb65RequestDto
    {
        Age = age,

        // Confusion flag from history; fallback to false
        Confusion = patientHistory.Confusion ?? false,

        // Urea from history; fallback to 5.0 mmol/L
        Urea = patientHistory.BloodUreaMmolPerL ?? 5.0m,

        // RespiratoryRate from history; fallback to 18
        RespiratoryRate = patientHistory.RespiratoryRate ?? 18,

        SystolicBP = systolic,
        DiastolicBP = diastolic
    };

    return await CalculateCurb65Async(request);
}

private (decimal systolic, decimal diastolic) ExtractBloodPressure(string? bloodPressure)
{
    if (string.IsNullOrEmpty(bloodPressure))
        return (120m, 80m);

    var parts = bloodPressure.Split('/');
    if (parts.Length == 2
        && decimal.TryParse(parts[0], out var s)
        && decimal.TryParse(parts[1], out var d))
    {
        return (s, d);
    }

    return (120m, 80m);
}


    }
