namespace BlazorApp1.Models.DTOs{
public class ChangeDoctorPasswordRequest
{
    public int DoctorId { get; set; }

    public string CurrentPassword { get; set; } = string.Empty;


    public string NewPassword { get; set; } = string.Empty;
}
}