using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Infrastructure.Authentication.Jwt
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Key { get; init; } = string.Empty;

        public string Issuer { get; init; } = string.Empty;

        public string Audience { get; init; } = string.Empty;

        public int ExpireMinutes { get; init; }

        public bool ValidateIssuer { get; init; } = true;

        public bool ValidateAudience { get; init; } = true;

        public bool ValidateLifetime { get; init; } = true;

        public bool ValidateIssuerSigningKey { get; init; } = true;
    }
}
