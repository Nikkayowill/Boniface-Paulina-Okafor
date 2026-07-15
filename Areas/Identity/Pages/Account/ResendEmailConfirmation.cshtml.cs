using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Okafor_.NET.Models;

namespace Okafor_.NET.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ResendEmailConfirmationModel> _logger;

    public ResendEmailConfirmationModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<ResendEmailConfirmationModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RequestCompleted { get; private set; }

    public sealed class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet(string? email = null)
    {
        Input.Email = email?.Trim() ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Email = (Input.Email ?? string.Empty).Trim();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                _logger.LogError("Unable to generate an email confirmation callback URL.");
                ModelState.AddModelError(string.Empty, "The confirmation email could not be sent right now. Please try again later.");
                return Page();
            }

            try
            {
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirmation email resend failed.");
                ModelState.AddModelError(string.Empty, "The confirmation email could not be sent right now. Please try again later.");
                return Page();
            }
        }

        RequestCompleted = true;
        return Page();
    }
}
