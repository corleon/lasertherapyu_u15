namespace LTU_U15.Services.Membership;

public interface IMembershipEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendHtmlAsync(string to, string subject, string body);
}
