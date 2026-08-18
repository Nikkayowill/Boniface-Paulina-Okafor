#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Okafor_.NET.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Shown after too many failed sign-in attempts trip the account lockout
    /// policy configured in <c>AddOkaforIdentityAndAuthorization</c>
    /// (5 failed attempts, 15-minute lockout window).
    /// </summary>
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
