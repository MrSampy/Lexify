using Lexify.Application.Abstractions;
using Lexify.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lexify.API.Filters;

public sealed class AdminOnlyFilter(ICurrentUserService currentUser) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (currentUser.Role != User.Roles.Admin && currentUser.Role != User.Roles.Moderator)
            context.Result = new ForbidResult();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
