namespace PmfApi.Application.Interfaces;

public interface IEmailSender
{
    
    bool IsConfigured { get; }

    Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct);
}