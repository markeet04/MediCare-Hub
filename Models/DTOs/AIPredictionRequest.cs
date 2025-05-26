namespace BlazorApp1.Models.DTOs
{
    public class AIPredictionRequest
    {

        public int PatientId { get; set; }

        public string PredictionType { get; set; } = string.Empty; // Risk or Treatment
        public Dictionary<string, object> InputData { get; set; } = new();
    }
}