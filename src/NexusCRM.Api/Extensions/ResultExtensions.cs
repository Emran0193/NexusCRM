using Microsoft.AspNetCore.Mvc;
using NexusCRM.Shared.Results;

namespace NexusCRM.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToProblem(result.Error!);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToProblem(result.Error!);
    }

    public static IActionResult ToCreatedResult<T>(this Result<T> result, string location)
    {
        if (result.IsSuccess)
        {
            return new CreatedResult(location, result.Value);
        }

        return ToProblem(result.Error!);
    }

    private static ObjectResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            "NotFound" => StatusCodes.Status404NotFound,
            "Validation" => StatusCodes.Status400BadRequest,
            "Conflict" => StatusCodes.Status409Conflict,
            "Forbidden" => StatusCodes.Status403Forbidden,
            "Unauthorized" or "MfaRequired" => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = status,
            Type = $"https://httpstatuses.com/{status}"
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
