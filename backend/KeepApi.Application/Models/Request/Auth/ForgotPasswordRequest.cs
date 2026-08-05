using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Application.Models.Request.Auth
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
