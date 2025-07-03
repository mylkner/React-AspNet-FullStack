using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Server.Models.Errors;

namespace Server.Middleware;

public class ExceptionMiddleware(IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        ProblemDetails errorRes = new();
        if (!env.IsDevelopment())
        {
            errorRes.Detail = "An unexpected error occurred.";
            errorRes.Status = (int)HttpStatusCode.InternalServerError;
            errorRes.Title = "Internal Server Error";
        }
        else
        {
            errorRes.Detail = exception.Message;
            errorRes.Status = GetStatusCode(exception);
            errorRes.Title = "An error has occured.";
        }

        if (exception is RefreshTokenError)
            httpContext.Response.Cookies.Delete("refreshToken");

        await httpContext.Response.WriteAsJsonAsync(errorRes, cancellationToken: cancellationToken);
        return true;
    }

    private static int GetStatusCode(Exception ex)
    {
        if (ex is RefreshTokenError refreshEx)
            ex = refreshEx.OriginalEx;
        return ex switch
        {
            BadHttpRequestException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ForbiddenException => (int)HttpStatusCode.Forbidden,
            NotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError,
        };
    }
}
