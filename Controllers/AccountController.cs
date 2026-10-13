using System.Security.Claims;
using GSCFacultyTimeMonitoring.Data;
using GSCFacultyTimeMonitoring.Models;
using GSCFacultyTimeMonitoring.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GSCFacultyTimeMonitoring.Controllers;

public class AccountController(AppDbContext db, IPasswordHasher<AdminUser> passwordHasher) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Admin");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalized = model.Username.Trim();
        var admin = await db.AdminUsers.SingleOrDefaultAsync(x => x.Username == normalized && x.IsActive);

        if (admin is null || passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.AdminUserId.ToString()),
            new(ClaimTypes.Name, admin.DisplayName),
            new(ClaimTypes.Role, "Administrator"),
            new("username", admin.Username),
            new("must_change_password", admin.MustChangePassword ? "true" : "false")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            });

        HttpContext.Session.SetInt32("AdminUserId", admin.AdminUserId);

        if (admin.MustChangePassword)
        {
            TempData["AdminInfo"] = "For security, please change the initial administrator password.";
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Admin");
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out var adminId))
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        var admin = await db.AdminUsers.FindAsync(adminId);
        if (admin is null || !admin.IsActive)
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        if (passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            return View(model);
        }

        admin.PasswordHash = passwordHasher.HashPassword(admin, model.NewPassword);
        admin.MustChangePassword = false;
        await db.SaveChangesAsync();

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["AdminSuccess"] = "Password changed successfully. Please sign in again with your new password.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
