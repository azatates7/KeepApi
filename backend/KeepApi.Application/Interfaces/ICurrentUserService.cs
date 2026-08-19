
namespace KeepApi.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }

        string? Username { get; }

        bool IsAuthenticated { get; }
    }
}
