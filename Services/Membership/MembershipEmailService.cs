using System.Net;
using System.Net.Mail;
using LTU_U15.Services.Site;

namespace LTU_U15.Services.Membership;

public sealed class MembershipEmailService : IMembershipEmailService
{
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly ILogger<MembershipEmailService> _logger;

    public MembershipEmailService(ISiteSettingsService siteSettingsService, ILogger<MembershipEmailService> logger)
    {
        _siteSettingsService = siteSettingsService;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
        => await SendCoreAsync(to, subject, body, isBodyHtml: false);

    public async Task SendHtmlAsync(string to, string subject, string body)
        => await SendCoreAsync(to, subject, body, isBodyHtml: true);

    private async Task SendCoreAsync(string to, string subject, string body, bool isBodyHtml)
    {
        var cfg = (await _siteSettingsService.GetAsync()).Email;
        if (string.IsNullOrWhiteSpace(cfg.Host) || string.IsNullOrWhiteSpace(cfg.From))
        {
            _logger.LogWarning("Membership email settings are not configured. Skipping email to {Email}", to);
            return;
        }

        using var message = new MailMessage(cfg.From, to, subject, body)
        {
            IsBodyHtml = isBodyHtml
        };
        message.BodyEncoding = System.Text.Encoding.UTF8;
        message.SubjectEncoding = System.Text.Encoding.UTF8;

        using var client = new SmtpClient
        {
            Host = cfg.Host,
            Port = cfg.Port,
            EnableSsl = cfg.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout = 600000,
            Credentials = new NetworkCredential(cfg.Username, cfg.Password)
        };
        client.ServicePoint.MaxIdleTime = 2;

        client.Send(message);
    }
}
