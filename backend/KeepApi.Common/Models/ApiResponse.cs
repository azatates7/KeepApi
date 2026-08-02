using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Common.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }

        public IEnumerable<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "")
            => new()
            {
                Success = true,
                Message = message,
                Data = data
            };

        public static ApiResponse<T> Fail(
            string message,
            IEnumerable<string>? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors
            };
    }
}
