using System.ComponentModel.DataAnnotations;

namespace LTU_U15.Models.Membership;

public sealed class ForgotUsernameViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
