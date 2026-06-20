using System.Diagnostics;
using Lexify.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ToActionResult(Result result) =>
        result.Status switch
        {
            ResultStatus.Ok => Ok(),
            ResultStatus.NotFound => NotFound(new { message = result.ErrorMessage }),
            ResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            ResultStatus.Failure => BadRequest(new { message = result.ErrorMessage }),
            _ => throw new UnreachableException($"Unhandled ResultStatus: {result.Status}")
        };

    protected IActionResult ToActionResult<T>(Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Ok => Ok(result.Value),
            ResultStatus.NotFound => NotFound(new { message = result.ErrorMessage }),
            ResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            ResultStatus.Failure => BadRequest(new { message = result.ErrorMessage }),
            _ => throw new UnreachableException($"Unhandled ResultStatus: {result.Status}")
        };

    protected IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (!result.IsSuccess)
            return ToActionResult(result);
        return onSuccess(result.Value!);
    }
}
