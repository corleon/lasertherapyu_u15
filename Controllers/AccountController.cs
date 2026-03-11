using System.Security.Cryptography;
using System.Text.Json;
using LTU_U15.Models.Membership;
using LTU_U15.Services.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Security;

namespace LTU_U15.Controllers;

[AllowAnonymous]
[Route("account")]
public sealed class AccountController : Controller
{
    private const string StandardMemberGroup = "Standard";

    private readonly IMemberService _memberService;
    private readonly IMemberSignInManager _memberSignInManager;
    private readonly UserManager<MemberIdentityUser> _memberUserManager;
    private readonly IMembershipEmailService _emailService;
    private readonly IMembershipNotificationService _membershipNotificationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IMemberService memberService,
        IMemberSignInManager memberSignInManager,
        UserManager<MemberIdentityUser> memberUserManager,
        IMembershipEmailService emailService,
        IMembershipNotificationService membershipNotificationService,
        ILogger<AccountController> logger)
    {
        _memberService = memberService;
        _memberSignInManager = memberSignInManager;
        _memberUserManager = memberUserManager;
        _emailService = emailService;
        _membershipNotificationService = membershipNotificationService;
        _logger = logger;
    }

    [HttpGet("registration")]
    public IActionResult Registration()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return Redirect("/members/my-profile/");
        }

        return View("Register", new RegisterMemberViewModel());
    }

    [HttpPost("registration")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registration(RegisterMemberViewModel model)
    {
        IMember? member = null;

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ",
                ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
            );

            _logger.LogWarning("Registration validation failed for {Username}. Errors: {Errors}", model.Username, validationErrors);
            var message = string.IsNullOrWhiteSpace(validationErrors)
                ? "Please fill in all required registration fields."
                : $"Please fix validation errors: {validationErrors}";
            SaveRegistrationDraft(model, message);
            return Redirect("/members/registration/");
        }

        if (_memberService.GetByUsername(model.Username) != null)
        {
            SaveRegistrationDraft(model, "Username already exists.");
            return Redirect("/members/registration/");
        }

        if (_memberService.GetMembersByEmail(model.Email).Any())
        {
            SaveRegistrationDraft(model, "Email already exists.");
            return Redirect("/members/registration/");
        }

        try
        {
            member = _memberService.CreateMemberWithIdentity(model.Username, model.Email, $"{model.FirstName} {model.LastName}", "Member");
            member.IsApproved = true;
            member.SetValue("firstName", model.FirstName);
            member.SetValue("lastName", model.LastName);
            member.SetValue("phoneNumber", model.PhoneNumber);
            member.SetValue("companyName", model.CompanyName);
            member.SetValue("address", model.Address);
            member.SetValue("address2", model.Address2);
            member.SetValue("city", model.City);
            member.SetValue("country", model.Country);
            member.SetValue("state", model.State);
            member.SetValue("zipPostalCode", model.ZipPostalCode);
            member.SetValue("customerType", model.CustomerType);
            member.SetValue("userType", model.UserType);
            member.SetValue("productOwned", model.ProductOwned);
            member.SetValue("productName", model.ProductName);
            member.SetValue("hearAboutUs", model.HearAboutUs);
            member.SetValue("optIn", model.OptIn);
            member.SetValue("acceptedTerms", model.AcceptedTerms);

            _memberService.Save(member);

            var identityUser = await _memberUserManager.FindByNameAsync(model.Username)
                               ?? await _memberUserManager.FindByEmailAsync(model.Email);
            if (identityUser == null)
            {
                throw new InvalidOperationException("Created member identity user was not found.");
            }

            var addPasswordResult = await _memberUserManager.AddPasswordAsync(identityUser, model.Password);
            if (!addPasswordResult.Succeeded)
            {
                TryDeleteMember(member);
                SaveRegistrationDraft(model, string.Join("; ", addPasswordResult.Errors.Select(x => x.Description)));
                return Redirect("/members/registration/");
            }

            var signInIdentifier = identityUser.UserName ?? model.Username;
            await EnsureStandardMemberGroupAsync(identityUser);
            await _memberSignInManager.PasswordSignInAsync(signInIdentifier, model.Password, isPersistent: true, lockoutOnFailure: true);

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                await _membershipNotificationService.SendRegistrationCompletedAsync(model, baseUrl);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Registration notification email sending failed for {Username}", model.Username);
            }
        }
        catch (Exception ex)
        {
            TryDeleteMember(member);
            _logger.LogError(ex, "Failed to register member {Username}", model.Username);
            var isUserSafeValidationMessage = ex is InvalidOperationException invalidOp
                                              && (invalidOp.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                                                  || invalidOp.Message.Contains("identity user", StringComparison.OrdinalIgnoreCase));
            SaveRegistrationDraft(model, isUserSafeValidationMessage ? ex.Message : "Registration failed. Please try again.");
            return Redirect("/members/registration/");
        }

        TempData["MembershipMessage"] = "Registration completed successfully.";
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return Redirect("/members/my-profile/");
    }

    [HttpGet("log-in")]
    public IActionResult LogIn([FromQuery(Name = "r")] string? returnUrl)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return Redirect("/members/my-profile/");
        }

        return View("Login", new LoginMemberViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("log-in")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogIn(LoginMemberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["MembershipMessage"] = "Please enter username and password.";
            return Redirect(BuildLoginPageUrl(model.ReturnUrl));
        }

        var identityUser = await _memberUserManager.FindByNameAsync(model.Username)
                           ?? await _memberUserManager.FindByEmailAsync(model.Username);

        var signInIdentifier = identityUser?.UserName ?? model.Username;
        var signInResult = await _memberSignInManager.PasswordSignInAsync(signInIdentifier, model.Password, isPersistent: true, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            TempData["MembershipMessage"] = "Incorrect username or password.";
            return Redirect(BuildLoginPageUrl(model.ReturnUrl));
        }

        if (await EnsureStandardMemberGroupAsync(identityUser))
        {
            await _memberSignInManager.SignOutAsync();
            signInResult = await _memberSignInManager.PasswordSignInAsync(signInIdentifier, model.Password, isPersistent: true, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                TempData["MembershipMessage"] = "Incorrect username or password.";
                return Redirect(BuildLoginPageUrl(model.ReturnUrl));
            }
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return Redirect("/members/my-profile/");
    }

    [HttpPost("log-out")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOut()
    {
        await _memberSignInManager.SignOutAsync();
        return Redirect("/");
    }

    [HttpGet("forgot-username")]
    public IActionResult ForgotUsername() => View(new ForgotUsernameViewModel());

    [HttpPost("forgot-username")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotUsername(ForgotUsernameViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["MembershipMessage"] = "Please enter a valid email.";
            return Redirect("/members/forgot-username/");
        }

        var member = _memberService.GetMembersByEmail(model.Email).FirstOrDefault();
        if (member != null)
        {
            try
            {
                await _emailService.SendAsync(
                    model.Email,
                    "Your Laser Therapy University username",
                    $"Your username is: {member.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot username email sending failed for {Email}", model.Email);
            }
        }

        TempData["MembershipMessage"] = "If this email exists, your username has been sent.";
        return Redirect("/members/forgot-username/");
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["MembershipMessage"] = "Please enter a valid email.";
            return Redirect("/members/forgot-password/");
        }

        var member = _memberService.GetMembersByEmail(model.Email).FirstOrDefault();
        if (member != null)
        {
            var temporaryPassword = GenerateTemporaryPassword();
            var identityUser = await _memberUserManager.FindByNameAsync(member.Username);
            if (identityUser != null)
            {
                var token = await _memberUserManager.GeneratePasswordResetTokenAsync(identityUser);
                var resetResult = await _memberUserManager.ResetPasswordAsync(identityUser, token, temporaryPassword);
                if (resetResult.Succeeded)
                {
                    try
                    {
                        await _emailService.SendAsync(
                            model.Email,
                            "Your Laser Therapy University temporary password",
                            $"Username: {member.Username}\nTemporary password: {temporaryPassword}\nPlease log in and change it.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Forgot password email sending failed for {Email}", model.Email);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not reset password for member {Username}: {Errors}", member.Username, string.Join("; ", resetResult.Errors.Select(x => x.Description)));
                }
            }
        }

        TempData["MembershipMessage"] = "If this email exists, a temporary password has been sent.";
        return Redirect("/members/forgot-password/");
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
        Span<char> result = stackalloc char[14];
        Span<byte> random = stackalloc byte[14];
        RandomNumberGenerator.Fill(random);

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = chars[random[i] % chars.Length];
        }

        return new string(result);
    }

    private void SaveRegistrationDraft(RegisterMemberViewModel model, string message)
    {
        TempData["MembershipMessage"] = message;
        TempData["RegistrationModel"] = JsonSerializer.Serialize(model);
    }

    private static string BuildLoginPageUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/members/log-in/";
        }

        return $"/members/log-in?r={Uri.EscapeDataString(returnUrl)}";
    }

    private async Task<bool> EnsureStandardMemberGroupAsync(MemberIdentityUser? identityUser)
    {
        if (identityUser == null || string.IsNullOrWhiteSpace(identityUser.UserName))
        {
            return false;
        }

        var username = identityUser.UserName;

        try
        {
            if (_memberService.GetAllRoles(username).Contains(StandardMemberGroup, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            _memberService.AssignRole(username, StandardMemberGroup);

            if (_memberService.GetAllRoles(username).Contains(StandardMemberGroup, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not add member {Username} to group {MemberGroup} via IMemberService", username, StandardMemberGroup);
        }

        try
        {
            if (await _memberUserManager.IsInRoleAsync(identityUser, StandardMemberGroup))
            {
                return false;
            }

            var addToRoleResult = await _memberUserManager.AddToRoleAsync(identityUser, StandardMemberGroup);
            if (addToRoleResult.Succeeded)
            {
                return true;
            }

            _logger.LogWarning(
                "Could not add member {Username} to group {MemberGroup}: {Errors}",
                username,
                StandardMemberGroup,
                string.Join("; ", addToRoleResult.Errors.Select(x => x.Description)));
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Member role assignment is not supported for {Username}", username);
        }

        return false;
    }

    private void TryDeleteMember(IMember? member)
    {
        if (member == null)
        {
            return;
        }

        try
        {
            _memberService.Delete(member);
        }
        catch (Exception deleteEx)
        {
            _logger.LogWarning(deleteEx, "Could not rollback member {MemberId} after failed registration", member.Id);
        }
    }
}
