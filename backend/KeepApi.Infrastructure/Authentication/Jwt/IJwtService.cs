using KeepApi.Data.Entity;

namespace KeepApi.Infrastructure.Authentication.Jwt
{
    public interface IJwtService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
    }
}
