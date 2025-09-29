using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using UserManagmentSystem.Web.Data;

namespace UserManagmentSystem.Web.Controllers;

public class BaseController : Controller
{
    private readonly AppDbContext _context;

    public BaseController(AppDbContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            base.OnActionExecuting(context);
            return;
        }

        var userEmailClaim = HttpContext.User.FindFirst(ClaimTypes.Email);
        if (userEmailClaim == null)
        {
            base.OnActionExecuting(context);
            return;
        }

        var user = _context.Users.FirstOrDefault(u => u.Email == userEmailClaim.Value);

        if (user != null && user.Status == Models.Entities.Status.Blocked)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        base.OnActionExecuting(context);
    }

}

