using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

using KeepApi.Data.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Authentication.Jwt
{
    public sealed class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtService(
            IOptions<JwtSettings> options,
            UserManager<ApplicationUser> userManager)
        {
            _settings = options.Value;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                new(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new(ClaimTypes.Name, user.UserName ?? string.Empty),

                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expires =
                DateTime.Now.AddMinutes(_settings.ExpireMinutes);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
 }
