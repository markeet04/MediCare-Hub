namespace BlazorApp1.Models
{
    public class User
    {
        public int Id { get; set; } // unique identifier
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
