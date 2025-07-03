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
            errorRes.Status = exception switch
            {
                BadHttpRequestException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ForbiddenException => (int)HttpStatusCode.Forbidden,
                NotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError,
            };
            errorRes.Title = "An error has occured.";
        }

        await httpContext.Response.WriteAsJsonAsync(errorRes, cancellationToken: cancellationToken);
        return true;
    }
}
