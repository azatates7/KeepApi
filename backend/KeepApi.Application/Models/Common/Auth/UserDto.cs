namespace KeepApi.Application.Models.Common.Auth
{
    public class UserDto
    {
        public Guid Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = [];
    }
}
