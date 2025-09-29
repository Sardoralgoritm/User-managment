using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using UserManagmentSystem.Web.Data;
using UserManagmentSystem.Web.Models;
using UserManagmentSystem.Web.Models.Entities;
using UserManagmentSystem.Web.Services;
using UserManagmentSystem.Web.Services.Interfaces;

namespace UserManagmentSystem.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    public AccountController(IUserService userService,
                             IEmailService emailService)
    {
        _userService = userService;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        var result = await _userService.LogInUserAsync(viewModel);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("", "Home");
        }

        TempData["ErrorMessage"] = result.Message;
        return View(viewModel);
    }


    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var res = await _userService.RegisterUserAsync(model);

        if (res.Success)
        {
            TempData["SuccessMessage"] = res.Message;
            return RedirectToAction("login", "account");
        }

        TempData["ErrorMessage"] = res.Message;
        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var resetToken = Guid.NewGuid().ToString("N");

        var result = await _emailService.SendPasswordResetAsync(model.Email, model.Email, resetToken);

        if (result)
        {
            TempData["ShowToast"] = true;
            return RedirectToAction("ForgotPassword");
        }

        TempData["ErrorMessage"] = "Failed to send reset link.";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid userId, string token)
    {
        if (string.IsNullOrEmpty(token) || userId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "The email is not found!";
            return RedirectToAction("Login");
        }

        var isValid = await _emailService.ValidatePasswordResetTokenAsync(userId, token);

        if (!isValid.Success)
        {
            TempData["ErrorMessage"] = isValid.Message;
            return RedirectToAction("Login");
        }

        var model = new ResetPasswordViewModel
        {
            Token = token,
            UserId = userId
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _emailService.ResetPasswordAsync(model.UserId, model.Token, model.NewPassword);

        if (!result)
        {
            TempData["ErrorMessage"] = "Invalid or expired reset link.";
            return View(model);
        }

        TempData["SuccessMessage"] = "Your password has been reset. You can now log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(Guid userId, string token)
    {
        var result = await _emailService.VerifyEmail(userId, token);

        if (result)
            return View("VerificationSuccess");

        return View("VerificationFailed");
    }
}
