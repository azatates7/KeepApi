namespace KeepApi.Application.Models.Response.Auth
{
    public class LoginResponse
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = [];

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
