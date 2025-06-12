using System;

namespace BlazorApp1.Helpers
{
    public static class Qrisk3Calculator
    {
        // QRISK-3 coefficients and baseline survival values based on published algorithm
        private static readonly decimal BaseSurvivalMale10Years = 0.977268040180206m;
        private static readonly decimal BaseSurvivalFemale10Years = 0.988876402378082m;

      
        public static decimal Calculate(
            int age,
            bool isMale,
            string ethnicity,
            decimal townsendScore, 
            bool smokingStatus,
            bool diabetesType1,
            bool diabetesType2,
            bool familyHistoryCVD,
            bool chronicKidneyDisease,
            bool atrialFibrillation,
            bool bloodPressureTreatment,
            bool rheumatoidArthritis,
            decimal systolicBP,
            decimal cholesterolHDLRatio,
            decimal bmi)
        {
            // Validate age range
            age = Math.Max(25, Math.Min(84, age));
            
            // Center continuous variables (approximate centering values from QRISK-3)
            decimal ageCentered = age - 55.0m;
            decimal townsendCentered = townsendScore - 0.0m;
            decimal systolicBPCentered = systolicBP - 130.0m;
            decimal cholesterolRatioCentered = cholesterolHDLRatio - 4.0m;
            decimal bmiCentered = bmi - 26.0m;

            decimal logHazard = 0m;

            if (isMale)
            {
                // Male coefficients (approximate values based on published research)
                logHazard += ageCentered * 0.0443318m;
                logHazard += (decimal)Math.Pow((double)ageCentered, 2) * 0.0000361m;
                
                // Ethnicity coefficients
                logHazard += GetEthnicityCoefficient(ethnicity, true);
                
                logHazard += townsendCentered * 0.0331538m;
                logHazard += (smokingStatus ? 1 : 0) * 0.5316107m;
                logHazard += (diabetesType1 ? 1 : 0) * 0.9454348m;
                logHazard += (diabetesType2 ? 1 : 0) * 0.4663267m;
                logHazard += (familyHistoryCVD ? 1 : 0) * 0.5470602m;
                logHazard += (chronicKidneyDisease ? 1 : 0) * 0.6186864m;
                logHazard += (atrialFibrillation ? 1 : 0) * 0.8398877m;
                logHazard += (bloodPressureTreatment ? 1 : 0) * 0.1933779m;
                logHazard += (rheumatoidArthritis ? 1 : 0) * 0.3111209m;
                logHazard += systolicBPCentered * 0.0096317m;
                logHazard += cholesterolRatioCentered * 0.1684397m;
                logHazard += bmiCentered * 0.0073716m;
                logHazard += (decimal)Math.Pow((double)bmiCentered, 2) * 0.0004077m;
            }
            else
            {
                // Female coefficients (approximate values based on published research)
                logHazard += ageCentered * 0.0730874m;
                logHazard += (decimal)Math.Pow((double)ageCentered, 2) * -0.0000328m;
                
                // Ethnicity coefficients
                logHazard += GetEthnicityCoefficient(ethnicity, false);
                
                logHazard += townsendCentered * 0.0269191m;
                logHazard += (smokingStatus ? 1 : 0) * 0.7034061m;
                logHazard += (diabetesType1 ? 1 : 0) * 1.2343425m;
                logHazard += (diabetesType2 ? 1 : 0) * 0.6968926m;
                logHazard += (familyHistoryCVD ? 1 : 0) * 0.4387883m;
                logHazard += (chronicKidneyDisease ? 1 : 0) * 0.4907757m;
                logHazard += (atrialFibrillation ? 1 : 0) * 1.2785425m;
                logHazard += (bloodPressureTreatment ? 1 : 0) * 0.1540293m;
                logHazard += (rheumatoidArthritis ? 1 : 0) * 0.2579415m;
                logHazard += systolicBPCentered * 0.0127586m;
                logHazard += cholesterolRatioCentered * 0.1530949m;
                logHazard += bmiCentered * -0.0018971m;
                logHazard += (decimal)Math.Pow((double)bmiCentered, 2) * 0.0009518m;
            }

            // Calculate 10-year risk using baseline survival
            decimal baseSurvival = isMale ? BaseSurvivalMale10Years : BaseSurvivalFemale10Years;
            decimal individualSurvival = (decimal)Math.Pow((double)baseSurvival, Math.Exp((double)logHazard));
            decimal risk = (1m - individualSurvival) * 100m;

            // Ensure risk is within reasonable bounds
            return Math.Max(0m, Math.Min(100m, risk));
        }

        private static decimal GetEthnicityCoefficient(string ethnicity, bool isMale)
        {
            if (string.IsNullOrEmpty(ethnicity))
                return 0m;

            return ethnicity.ToLower() switch
            {
                "indian" => isMale ? 0.4498674m : 0.3835521m,
                "pakistani" => isMale ? 0.7877854m : 0.5432419m,
                "bangladeshi" => isMale ? 0.3840996m : 0.8009063m,
                "other asian" => isMale ? 0.3543645m : 0.1682606m,
                "black caribbean" => isMale ? 0.2306590m : -0.0417948m,
                "black african" => isMale ? -0.0551322m : -0.2779804m,
                "chinese" => isMale ? 0.1068192m : -0.2339150m,
                "other ethnic group" => isMale ? 0.2519657m : 0.0431053m,
                _ => 0m // White or not recorded
            };
        }

        public static string GetRiskCategory(decimal riskPercentage)
        {
            return riskPercentage switch
            {
                < 10m => "Low Risk",
                >= 10m and < 20m => "Moderate Risk",
                >= 20m => "High Risk"
            };
        }
    }

    public static class FindriscCalculator
    {
        /// <summary>
        /// Calculates raw FINDRISC points (0–26) from individual risk factors.
        /// Uses the official point assignment tables per the FINDRISC guidelines.
        /// </summary>
        public static int CalculatePoints(
            int age,
            decimal bmi,
            decimal waistCircumference,
            bool dailyPhysicalActivity,
            bool eatsVegetablesDaily,
            bool takesBloodPressureMedication,
            bool historyHighBloodGlucose,
            bool familyHistoryDiabetes,
            bool isFemale = false) // Added gender parameter for waist circumference
        {
            int points = 0;

            // 1) Age:
            //    < 45      → 0 points
            //    45–54     → 2 points
            //    55–64     → 3 points
            //    ≥ 65      → 4 points
            if (age >= 45 && age <= 54) points += 2;
            else if (age >= 55 && age <= 64) points += 3;
            else if (age >= 65) points += 4;

            // 2) BMI:
            //    < 25      → 0
            //    25–30     → 1
            //    > 30      → 3
            if (bmi >= 25m && bmi < 30m) points += 1;
            else if (bmi >= 30m) points += 3;

            // 3) Waist circumference (sex-specific):
            //    Men:   < 94 cm → 0; 94–102 cm → 3; > 102 cm → 4
            //    Women: < 80 cm → 0; 80–88 cm → 3; > 88 cm → 4
            if (isFemale)
            {
                if (waistCircumference >= 80m && waistCircumference <= 88m) points += 3;
                else if (waistCircumference > 88m) points += 4;
            }
            else
            {
                if (waistCircumference >= 94m && waistCircumference <= 102m) points += 3;
                else if (waistCircumference > 102m) points += 4;
            }

   
            if (!dailyPhysicalActivity) points += 2;


            if (!eatsVegetablesDaily) points += 1;

            if (takesBloodPressureMedication) points += 2;

            // 7) History of high blood glucose:
            //    Yes → 5; No → 0
            if (historyHighBloodGlucose) points += 5;

            // 8) Family history of diabetes:
            //    No family history → 0
            //    Grandparent/aunt/uncle → 3
            //    Parent or sibling → 5
            // Since we only have a boolean, treat "true" as the higher risk (parent/sibling).
            if (familyHistoryDiabetes) points += 5;

            return Math.Max(0, Math.Min(26, points));
        }

        /// <summary>
        /// Overload for backward compatibility without gender parameter
        /// </summary>
        public static int CalculatePoints(
            int age,
            decimal bmi,
            decimal waistCircumference,
            bool dailyPhysicalActivity,
            bool eatsVegetablesDaily,
            bool takesBloodPressureMedication,
            bool historyHighBloodGlucose,
            bool familyHistoryDiabetes)
        {
            // Default to male thresholds for backward compatibility
            return CalculatePoints(age, bmi, waistCircumference, dailyPhysicalActivity,
                eatsVegetablesDaily, takesBloodPressureMedication, historyHighBloodGlucose,
                familyHistoryDiabetes, false);
        }

        /// <summary>
        /// Maps raw FINDRISC points to a 10-year Type 2 diabetes risk percentage.
        /// Uses the official lookup table from Finnish Diabetes Risk Score.
        /// </summary>
        public static decimal MapPointsToPercentage(int points)
        {
            return points switch
            {
                <= 6 => 1m,
                >= 7 and <= 11 => 4m,
                >= 12 and <= 14 => 17m,
                >= 15 and <= 20 => 33m,
                >= 21 => 50m
            };
        }

        /// <summary>
        /// Maps raw FINDRISC points to a descriptive category.
        /// </summary>
        public static string GetRiskCategory(int points)
        {
            return points switch
            {
                <= 6 => "Low Risk",
                >= 7 and <= 11 => "Slightly Elevated Risk",
                >= 12 and <= 14 => "Moderate Risk",
                >= 15 and <= 20 => "High Risk",
                >= 21 => "Very High Risk"
            };
        }
    }

    public static class Curb65Calculator
    {
        /// <summary>
        /// Calculates CURB-65 score (0–5) for pneumonia severity assessment.
        /// Each criterion contributes 1 point if present.
        /// </summary>
        public static int CalculateScore(
            int age,
            bool confusion,
            decimal urea,
            int respiratoryRate,
            decimal systolicBP,
            decimal diastolicBP)
        {
            int score = 0;

            // C: Confusion
            if (confusion) score++;

            // U: Urea > 7 mmol/L (BUN > 19 mg/dL)
            if (urea > 7m) score++;

            // R: Respiratory rate ≥ 30/min
            if (respiratoryRate >= 30) score++;

            // B: Blood pressure (systolic < 90 mmHg OR diastolic ≤ 60 mmHg)
            if (systolicBP < 90m || diastolicBP <= 60m) score++;

            // 65: Age ≥ 65 years
            if (age >= 65) score++;

            return Math.Max(0, Math.Min(5, score));
        }

        /// <summary>
        /// Maps CURB-65 score to approximate 30-day mortality risk percentage.
        /// Based on Lim WS, et al. Thorax 2003 and subsequent validation studies.
        /// </summary>
        public static decimal GetMortalityRisk(int score)
        {
            return score switch
            {
                0 => 0.7m,
                1 => 2.1m,
                2 => 9.2m,
                3 => 14.5m,
                4 => 40.0m,
                5 => 57.0m,
                _ => 0m
            };
        }

        /// <summary>
        /// Returns a textual risk category and recommended level of care 
        /// based on CURB-65 score according to British Thoracic Society guidelines.
        /// </summary>
        public static (string riskCategory, string recommendedCare) GetRiskAssessment(int score)
        {
            return score switch
            {
                0 or 1 => ("Low Risk", "Consider home treatment"),
                2 => ("Moderate Risk", "Consider hospital admission or close outpatient monitoring"),
                >= 3 => ("High Risk", "Urgent hospital admission – consider ICU referral"),
                _ => ("Unknown", "Unable to assess")
            };
        }
    }
}