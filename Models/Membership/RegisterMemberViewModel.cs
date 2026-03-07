using System.ComponentModel.DataAnnotations;

namespace LTU_U15.Models.Membership;

public sealed class RegisterMemberViewModel
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CompanyName { get; set; }

    [Required, StringLength(100)]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Address2 { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [StringLength(100)]
    public string? State { get; set; }

    [Required, StringLength(20)]
    public string ZipPostalCode { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string CustomerType { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string UserType { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string ProductOwned { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ProductName { get; set; }

    [StringLength(120)]
    public string? HearAboutUs { get; set; }

    [Required, StringLength(50, MinimumLength = 4)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 10, ErrorMessage = "Password must be at least 10 characters long and at most 100."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool OptIn { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept Terms & Conditions")]
    public bool AcceptedTerms { get; set; }
}
