using BlazorApp1.Models;

namespace BlazorApp1.Services
{
    public class UserService
    {
        private List<User> users = new()
        {
            new User { Id = 1, Username = "admin", Role = "Admin" },
            new User { Id = 2, Username = "dr.john", Role = "Doctor" },
        };

        public List<User> GetAllUsers() => users;

        public void AddUser(User user)
        {
            user.Id = users.Max(u => u.Id) + 1;
            users.Add(user);
        }

        public void UpdateUser(User updatedUser)
        {
            var existing = users.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (existing is not null)
            {
                existing.Username = updatedUser.Username;
                existing.Role = updatedUser.Role;
            }
        }

        public void DeleteUser(int id)
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user is not null)
                users.Remove(user);
        }
    }
}
