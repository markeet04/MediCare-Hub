using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace BlazorApp1.Authentication
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public AuthStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
                
                if (userSession == null)
                    return await Task.FromResult(new AuthenticationState(_anonymous));

                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                    new Claim(ClaimTypes.Name, userSession.FullName),
                    new Claim(ClaimTypes.Role, userSession.Role),
                    new Claim("UserName", userSession.UserName)
                }, "CustomAuth"));

                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthStateProvider error: {ex.Message}");
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        public async Task MarkUserAsAuthenticatedAsync(ClaimsPrincipal user)
        {
            try
            {
                var userSession = new UserSession
                {
                    UserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                    UserName = user.FindFirst("UserName")?.Value ?? "",
                    FullName = user.FindFirst(ClaimTypes.Name)?.Value ?? "",
                    Role = user.FindFirst(ClaimTypes.Role)?.Value ?? ""
                };

                // Set session storage
                await _sessionStorage.SetAsync("UserSession", userSession);
                
                // Create authenticated state
                var authState = new AuthenticationState(user);
                
                // Notify of the change
                NotifyAuthenticationStateChanged(Task.FromResult(authState));
                
                // Add a small delay to ensure the state change is processed
                await Task.Delay(50);
                
                Console.WriteLine($"User authenticated: {userSession.UserName}, Role: {userSession.Role}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkUserAsAuthenticatedAsync: {ex.Message}");
                throw;
            }
        }

        public async Task MarkUserAsLoggedOutAsync()
        {
            try
            {
                await _sessionStorage.DeleteAsync("UserSession");
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                Console.WriteLine("User logged out");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkUserAsLoggedOutAsync: {ex.Message}");
            }
        }

        public async Task<UserSession?> GetCurrentUserSessionAsync()
        {
            try
            {
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                return userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current user session: {ex.Message}");
                return null;
            }
        }
    }

    public class UserSession
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}