using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public abstract class AdminBaseController : Controller
{
    /// <summary>
    /// Rows per page for every admin register (Index) view. Shared so paging
    /// stays identical across all admin list endpoints; referenced as
    /// <c>AdminBaseController.DefaultPageSize</c> even by controllers that
    /// don't inherit this base (they still need [Area("Admin")]/[Authorize]
    /// set up differently), so this is a plain const rather than a protected
    /// member.
    /// </summary>
    public const int DefaultPageSize = 25;
}
