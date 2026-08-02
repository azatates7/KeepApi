using KeepApi.Application.Models.Common.Auth;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Models.Response.Auth;

namespace KeepApi.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);

        Task RegisterAsync(RegisterRequest request);

        Task<UserDto> MeAsync(Guid userId);
    }
}
