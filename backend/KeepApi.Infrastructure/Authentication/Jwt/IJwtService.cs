using System;
using KeepApi.Data.Entity;
using System;

namespace KeepApi.Infrastructure.Authentication.Jwt
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
    }

    public interface IJwtService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
    }
}
