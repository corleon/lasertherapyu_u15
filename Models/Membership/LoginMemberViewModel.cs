using System.ComponentModel.DataAnnotations;

namespace LTU_U15.Models.Membership;

public sealed class LoginMemberViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
