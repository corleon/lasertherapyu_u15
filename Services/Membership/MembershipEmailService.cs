using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace LTU_U15.Services.Membership;

public sealed class MembershipEmailService : IMembershipEmailService
{
    private readonly IOptions<MembershipEmailSettings> _settings;
    private readonly ILogger<MembershipEmailService> _logger;

    public MembershipEmailService(IOptions<MembershipEmailSettings> settings, ILogger<MembershipEmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var cfg = _settings.Value;
        if (string.IsNullOrWhiteSpace(cfg.Host) || string.IsNullOrWhiteSpace(cfg.From))
        {
            _logger.LogWarning("Membership email settings are not configured. Skipping email to {Email}", to);
            return;
        }

        using var message = new MailMessage(cfg.From, to, subject, body)
        {
            IsBodyHtml = false
        };

        using var client = new SmtpClient(cfg.Host, cfg.Port)
        {
            EnableSsl = cfg.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(cfg.Username, cfg.Password)
        };

        await client.SendMailAsync(message);
    }
}
