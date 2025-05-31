using BlazorApp1.Models.DTOs;
namespace BlazorApp1.Services.Interfaces
{
    public interface IQriskService
    {
        Task<QriskResponseDto> CalculateQriskAsync(QriskRequestDto request);
        Task<QriskResponseDto> CalculateFromPatientHistoryAsync(PatientHistoryDto patientHistory, PatientProfileDto patientProfile);
    }

    public interface IFindriscService
    {
        Task<FindriscResponseDto> CalculateFindriscAsync(FindriscRequestDto request);
        Task<FindriscResponseDto> CalculateFromPatientHistoryAsync(PatientHistoryDto patientHistory, PatientProfileDto patientProfile);
    }

    public interface ICurb65Service
    {
        Task<Curb65ResponseDto> CalculateCurb65Async(Curb65RequestDto request);
        Task<Curb65ResponseDto> CalculateFromPatientHistoryAsync(PatientHistoryDto patientHistory, PatientProfileDto patientProfile);
    }
}