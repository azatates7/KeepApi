using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace KeepApi.Infrastructure.Email
{
    public class SmtpSettings
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }

    public sealed class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public SmtpEmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.User, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            using var message = new MailMessage(_settings.From, to, subject, body);

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}