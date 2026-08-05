using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Application.Models.Request.Auth
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}
