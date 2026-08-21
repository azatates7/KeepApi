using KeepApi.Application.Interfaces;
using KeepApi.Application.Models.Request.Auth;
using KeepApi.Application.Models.Response.Auth;
using KeepApi.Common.Models;
using KeepApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace KeepApi.Tests
{
    public class LoginTests
    {
        private class FakeAuthService : IAuthService
        {
            public Task<LoginResponse> LoginAsync(LoginRequest request)
            {
                if (request.UserNameOrEmail == "user@example.com" && request.Password == "correct")
                {
                    return Task.FromResult(new LoginResponse
                    {
                        UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        UserName = "user",
                        Email = "user@example.com",
                        FullName = "Test User",
                        PreferredLanguage = "en",
                        Roles = new System.Collections.Generic.List<string> { "User" },
                        Token = "fake-jwt-token",
                        ExpiresAt = DateTime.UtcNow.AddHours(1)
                    });
                }

                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // The following methods are not used by these tests and can remain unimplemented.
            public Task<LoginResponse> ExternalLoginAsync(string provider, Application.Models.Request.Auth.ExternalLoginRequest request) => throw new NotImplementedException();
            public Task RegisterAsync(Application.Models.Request.Auth.RegisterRequest request) => throw new NotImplementedException();
            public Task<Application.Models.Common.Auth.UserDto> MeAsync(Guid userId) => throw new NotImplementedException();
            public Task UpdateLanguageAsync(Guid userId, string language) => throw new NotImplementedException();
            public Task ForgotPasswordAsync(Application.Models.Request.Auth.ForgotPasswordRequest request) => throw new NotImplementedException();
            public Task ResetPasswordAsync(Application.Models.Request.Auth.ResetPasswordRequest request) => throw new NotImplementedException();
            public Task VerifyEmailAsync(Application.Models.Request.Auth.VerifyEmailRequest request) => throw new NotImplementedException();

            public Task<LoginResponse> RefreshTokenAsync(string refreshToken)
            {
                throw new NotImplementedException();
            }

            public Task RevokeRefreshTokenAsync(string refreshToken)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task Login_Success_ReturnsOkAndToken()
        {
            // Arrange
            var authService = new FakeAuthService();
            var controller = new AuthController(authService);
            var request = new LoginRequest
            {
                UserNameOrEmail = "user@example.com",
                Password = "correct",
                RememberMe = false
            };

            // Act
            var action = await controller.Login(request);

            // Assert
            Assert.IsType<OkObjectResult>(action.Result);
            var ok = action.Result as OkObjectResult;
            Assert.NotNull(ok);
            var api = Assert.IsType<ApiResponse<LoginResponse>>(ok.Value);
            Assert.True(api.Success);
            Assert.NotNull(api.Data);
            Assert.Equal("fake-jwt-token", api.Data.Token);
        }

        [Fact]
        public async Task Login_WrongCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var authService = new FakeAuthService();
            var controller = new AuthController(authService);
            var request = new LoginRequest
            {
                UserNameOrEmail = "user@example.com",
                Password = "wrong",
                RememberMe = false
            };

            // Act
            var action = await controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(action.Result);
            var unauthorized = action.Result as UnauthorizedObjectResult;
            Assert.NotNull(unauthorized);
            var api = Assert.IsType<ApiResponse<LoginResponse>>(unauthorized.Value);
            Assert.False(api.Success);
            Assert.Equal("Invalid credentials", api.Message);
        }
    }
}
