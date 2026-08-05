using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Application.Models.Request.Auth
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;

        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
