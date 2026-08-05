using System;
using KeepApi.Data.Entity;
using System;

namespace KeepApi.Infrastructure.Authentication.Jwt
{
    public interface IJwtService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
    }
}
