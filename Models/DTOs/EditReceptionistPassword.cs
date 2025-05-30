namespace BlazorApp1.Models.DTOs{
public class EditReceptionistPassword
{
    public int ReceptionistId { get; set; }

    public string CurrentPassword { get; set; } = string.Empty;


    public string NewPassword { get; set; } = string.Empty;
}
}