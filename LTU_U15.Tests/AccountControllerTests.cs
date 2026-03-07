using LTU_U15.Controllers;
using LTU_U15.Models.Membership;
using LTU_U15.Services.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Security;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace LTU_U15.Tests;

public class AccountControllerTests
{
    [Fact]
    public void Registration_Get_ReturnsRegisterView()
    {
        var sut = CreateController();

        var result = sut.Registration();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Register", view.ViewName);
        Assert.IsType<RegisterMemberViewModel>(view.Model);
    }

    [Fact]
    public async Task Registration_Post_InvalidModel_RedirectsWithMessage()
    {
        var sut = CreateController();
        sut.ModelState.AddModelError("FirstName", "Required");

        var result = await sut.Registration(new RegisterMemberViewModel());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/registration", redirect.Url);
        Assert.Contains("Please fix validation errors", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task Registration_Post_DuplicateUsername_RedirectsWithMessage()
    {
        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetByUsername("dup-user")).Returns(Mock.Of<IMember>());

        var sut = CreateController(memberService: memberService);
        var model = ValidRegistrationModel();
        model.Username = "dup-user";

        var result = await sut.Registration(model);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/registration", redirect.Url);
        Assert.Equal("Username already exists.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task Registration_Post_DuplicateEmail_RedirectsWithMessage()
    {
        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetByUsername(It.IsAny<string>())).Returns((IMember?)null);
        memberService.Setup(x => x.GetMembersByEmail(It.IsAny<string>())).Returns(new[] { Mock.Of<IMember>() });

        var sut = CreateController(memberService: memberService);
        var model = ValidRegistrationModel();
        model.Email = "dup@example.com";

        var result = await sut.Registration(model);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/registration", redirect.Url);
        Assert.Equal("Email already exists.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task Registration_Post_WhenCreateThrows_RedirectsWithFailureMessage()
    {
        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetByUsername(It.IsAny<string>())).Returns((IMember?)null);
        memberService.Setup(x => x.GetMembersByEmail(It.IsAny<string>())).Returns(Array.Empty<IMember>());
        memberService.Setup(x => x.CreateMemberWithIdentity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("boom"));

        var sut = CreateController(memberService: memberService);

        var result = await sut.Registration(ValidRegistrationModel());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/registration", redirect.Url);
        Assert.Equal("Registration failed. Please try again.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task Registration_Post_ValidModel_CreatesMemberAndRedirectsToProfile()
    {
        var member = new Mock<IMember>();

        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetByUsername(It.IsAny<string>())).Returns((IMember?)null);
        memberService.Setup(x => x.GetMembersByEmail(It.IsAny<string>())).Returns(Array.Empty<IMember>());
        memberService
            .Setup(x => x.CreateMemberWithIdentity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "Member"))
            .Returns(member.Object);

        var identityUser = new MemberIdentityUser
        {
            UserName = "john-doe",
            Email = "john@example.com"
        };

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByNameAsync("john-doe")).ReturnsAsync(identityUser);
        userManager
            .Setup(x => x.AddPasswordAsync(identityUser, "Password123!"))
            .ReturnsAsync(IdentityResult.Success);

        var signIn = new Mock<IMemberSignInManager>();
        signIn.Setup(x => x.PasswordSignInAsync("john-doe", "Password123!", true, true)).ReturnsAsync(SignInResult.Success);

        var sut = CreateController(memberService: memberService, signInManager: signIn, userManager: userManager);

        var result = await sut.Registration(ValidRegistrationModel());

        memberService.Verify(x => x.Save(member.Object), Times.Once);
        userManager.Verify(x => x.AddPasswordAsync(identityUser, "Password123!"), Times.Once);
        signIn.Verify(x => x.PasswordSignInAsync("john-doe", "Password123!", true, true), Times.Once);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/my-profile", redirect.Url);
        Assert.Equal("Registration completed successfully.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public void LogIn_Get_ReturnsLoginViewModelWithReturnUrl()
    {
        var sut = CreateController();

        var result = sut.LogIn("/cart");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Login", view.ViewName);
        var model = Assert.IsType<LoginMemberViewModel>(view.Model);
        Assert.Equal("/cart", model.ReturnUrl);
    }

    [Fact]
    public async Task LogIn_Post_InvalidModel_RedirectsToMembersLogin()
    {
        var sut = CreateController();
        sut.ModelState.AddModelError("Username", "Required");

        var result = await sut.LogIn(new LoginMemberViewModel());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/log-in", redirect.Url);
        Assert.Equal("Please enter username and password.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task LogIn_Post_BadCredentials_RedirectsToMembersLogin()
    {
        var signIn = new Mock<IMemberSignInManager>();
        signIn.Setup(x => x.PasswordSignInAsync("user", "bad", true, true)).ReturnsAsync(SignInResult.Failed);

        var sut = CreateController(signInManager: signIn);

        var result = await sut.LogIn(new LoginMemberViewModel { Username = "user", Password = "bad" });

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/log-in", redirect.Url);
        Assert.Equal("Incorrect username or password.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task LogIn_Post_SuccessWithLocalReturnUrl_RedirectsToReturnUrl()
    {
        var signIn = new Mock<IMemberSignInManager>();
        signIn.Setup(x => x.PasswordSignInAsync("user", "good", true, true)).ReturnsAsync(SignInResult.Success);

        var sut = CreateController(signInManager: signIn);

        var result = await sut.LogIn(new LoginMemberViewModel { Username = "user", Password = "good", ReturnUrl = "/members/my-profile" });

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/my-profile", redirect.Url);
    }

    [Fact]
    public async Task LogIn_Post_SuccessWithoutReturnUrl_RedirectsToProfile()
    {
        var signIn = new Mock<IMemberSignInManager>();
        signIn.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, true)).ReturnsAsync(SignInResult.Success);

        var sut = CreateController(signInManager: signIn);

        var result = await sut.LogIn(new LoginMemberViewModel { Username = "user", Password = "good" });

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/my-profile", redirect.Url);
    }

    [Fact]
    public async Task LogOut_Post_SignsOutAndRedirectsHome()
    {
        var signIn = new Mock<IMemberSignInManager>();
        signIn.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        var sut = CreateController(signInManager: signIn);

        var result = await sut.LogOut();

        signIn.Verify(x => x.SignOutAsync(), Times.Once);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public void ForgotUsername_Get_ReturnsViewWithModel()
    {
        var sut = CreateController();

        var result = sut.ForgotUsername();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ForgotUsernameViewModel>(view.Model);
    }

    [Fact]
    public async Task ForgotUsername_Post_InvalidModel_RedirectsWithMessage()
    {
        var sut = CreateController();
        sut.ModelState.AddModelError("Email", "Required");

        var result = await sut.ForgotUsername(new ForgotUsernameViewModel());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-username", redirect.Url);
        Assert.Equal("Please enter a valid email.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task ForgotUsername_Post_SendsEmailWhenMemberFound()
    {
        var member = new Mock<IMember>();
        member.SetupGet(x => x.Username).Returns("member-user");

        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetMembersByEmail(It.IsAny<string>())).Returns(new[] { member.Object });

        var emailService = new Mock<IMembershipEmailService>();
        emailService.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateController(memberService: memberService, emailService: emailService);

        var result = await sut.ForgotUsername(new ForgotUsernameViewModel { Email = "found@example.com" });

        emailService.Verify(x => x.SendAsync("found@example.com", It.Is<string>(s => s.Contains("username")), It.Is<string>(b => b.Contains("member-user"))), Times.Once);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-username", redirect.Url);
        Assert.Equal("If this email exists, your username has been sent.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task ForgotUsername_Post_WhenMemberNotFound_DoesNotSendEmail()
    {
        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetMembersByEmail(It.IsAny<string>())).Returns(Array.Empty<IMember>());

        var emailService = new Mock<IMembershipEmailService>();
        var sut = CreateController(memberService: memberService, emailService: emailService);

        var result = await sut.ForgotUsername(new ForgotUsernameViewModel { Email = "none@example.com" });

        emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-username", redirect.Url);
        Assert.Equal("If this email exists, your username has been sent.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public void ForgotPassword_Get_ReturnsViewWithModel()
    {
        var sut = CreateController();

        var result = sut.ForgotPassword();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ForgotPasswordViewModel>(view.Model);
    }

    [Fact]
    public async Task ForgotPassword_Post_InvalidModel_RedirectsWithMessage()
    {
        var sut = CreateController();
        sut.ModelState.AddModelError("Email", "Required");

        var result = await sut.ForgotPassword(new ForgotPasswordViewModel());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-password", redirect.Url);
        Assert.Equal("Please enter a valid email.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task ForgotPassword_Post_WhenMemberNotFound_StillRedirectsWithGenericMessage()
    {
        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetMembersByEmail("none@example.com")).Returns(Array.Empty<IMember>());

        var sut = CreateController(memberService: memberService);

        var result = await sut.ForgotPassword(new ForgotPasswordViewModel { Email = "none@example.com" });

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-password", redirect.Url);
        Assert.Equal("If this email exists, a temporary password has been sent.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task ForgotPassword_Post_WhenMemberFoundAndResetSucceeded_SendsEmail()
    {
        var member = new Mock<IMember>();
        member.SetupGet(x => x.Username).Returns("member-user");

        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetMembersByEmail("found@example.com")).Returns(new[] { member.Object });

        var identityUser = new MemberIdentityUser
        {
            UserName = "member-user",
            Email = "found@example.com"
        };

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByNameAsync("member-user")).ReturnsAsync(identityUser);
        userManager.Setup(x => x.GeneratePasswordResetTokenAsync(identityUser)).ReturnsAsync("token-123");
        userManager
            .Setup(x => x.ResetPasswordAsync(identityUser, "token-123", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var emailService = new Mock<IMembershipEmailService>();
        emailService.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = CreateController(memberService: memberService, userManager: userManager, emailService: emailService);

        var result = await sut.ForgotPassword(new ForgotPasswordViewModel { Email = "found@example.com" });

        userManager.Verify(x => x.ResetPasswordAsync(identityUser, "token-123", It.IsAny<string>()), Times.Once);
        emailService.Verify(x => x.SendAsync("found@example.com", It.Is<string>(s => s.Contains("temporary password")), It.Is<string>(b => b.Contains("member-user"))), Times.Once);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-password", redirect.Url);
        Assert.Equal("If this email exists, a temporary password has been sent.", sut.TempData["MembershipMessage"]?.ToString());
    }

    [Fact]
    public async Task ForgotPassword_Post_WhenResetFails_DoesNotSendEmail()
    {
        var member = new Mock<IMember>();
        member.SetupGet(x => x.Username).Returns("member-user");

        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetMembersByEmail("found@example.com")).Returns(new[] { member.Object });

        var identityUser = new MemberIdentityUser
        {
            UserName = "member-user",
            Email = "found@example.com"
        };

        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByNameAsync("member-user")).ReturnsAsync(identityUser);
        userManager.Setup(x => x.GeneratePasswordResetTokenAsync(identityUser)).ReturnsAsync("token-123");
        userManager
            .Setup(x => x.ResetPasswordAsync(identityUser, "token-123", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "reset failed" }));

        var emailService = new Mock<IMembershipEmailService>();

        var sut = CreateController(memberService: memberService, userManager: userManager, emailService: emailService);

        var result = await sut.ForgotPassword(new ForgotPasswordViewModel { Email = "found@example.com" });

        emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/members/forgot-password", redirect.Url);
        Assert.Equal("If this email exists, a temporary password has been sent.", sut.TempData["MembershipMessage"]?.ToString());
    }

    private static RegisterMemberViewModel ValidRegistrationModel() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com",
        PhoneNumber = "1234567890",
        Address = "123 Main st",
        City = "Chicago",
        Country = "United States",
        ZipPostalCode = "60601",
        CustomerType = "laseru\\Consumer",
        UserType = "Prospective Customer",
        ProductOwned = "GameDay Laser",
        Username = "john-doe",
        Password = "Password123!",
        ConfirmPassword = "Password123!",
        AcceptedTerms = true
    };

    private static AccountController CreateController(
        Mock<IMemberService>? memberService = null,
        Mock<IMemberSignInManager>? signInManager = null,
        Mock<UserManager<MemberIdentityUser>>? userManager = null,
        Mock<IMembershipEmailService>? emailService = null,
        Mock<ILogger<AccountController>>? logger = null)
    {
        if (memberService == null)
        {
            memberService = new Mock<IMemberService>();
            memberService.Setup(x => x.GetMembersByEmail(It.IsAny<string>())).Returns(Array.Empty<IMember>());
        }
        signInManager ??= new Mock<IMemberSignInManager>();
        userManager ??= CreateUserManagerMock();
        emailService ??= new Mock<IMembershipEmailService>();
        logger ??= new Mock<ILogger<AccountController>>();

        var controller = new AccountController(
            memberService.Object,
            signInManager.Object,
            userManager.Object,
            emailService.Object,
            logger.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        controller.Url = Mock.Of<IUrlHelper>(x => x.IsLocalUrl(It.IsAny<string>()) == true);

        return controller;
    }

    private static Mock<UserManager<MemberIdentityUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<MemberIdentityUser>>();
        return new Mock<UserManager<MemberIdentityUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
