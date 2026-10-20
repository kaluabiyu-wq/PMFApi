using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PmfApi.Application.Interfaces;

namespace PmfApi.Infrastructure.Persistence.Services;


public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Email:Host"])
        && !string.IsNullOrWhiteSpace(configuration["Email:From"]);

    public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not sent to {To}: the Email:Host / Email:From settings are missing", to);
            return false;
        }

        try
        {
            var port = int.TryParse(configuration["Email:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(configuration["Email:EnableSsl"], out var ssl) || ssl;

            using var client = new SmtpClient(configuration["Email:Host"], port) { EnableSsl = enableSsl };

            var user = configuration["Email:User"];
            if (!string.IsNullOrEmpty(user))
                client.Credentials = new NetworkCredential(user, configuration["Email:Password"]);

            using var message = new MailMessage(configuration["Email:From"]!, to, subject, body);
            await client.SendMailAsync(message, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sending email to {To} failed", to);
            return false;
        }
    }
}