using KeepApi.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace KeepApi.Infrastructure.Authentication.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?
                .User
                .Identity?
                .IsAuthenticated == true;

        public Guid UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?
                    .User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(userId, out var result))
                {
                    return result;
                }

                throw new InvalidOperationException("Current user does not have a valid UserId.");
            }
        }

        public string? Username
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;

                if (user?.Identity?.IsAuthenticated != true)
                    return null;

                return user.FindFirstValue(ClaimTypes.Name)
                       ?? user.FindFirstValue("preferred_username")
                       ?? user.FindFirstValue(ClaimTypes.Email);
            }
        }
    }
}