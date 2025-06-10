using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Server.Middleware;

public class ExceptionMiddleware(IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        ProblemDetails errorRes = new()
        {
            Detail = env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Internal Server Error",
        };

        await httpContext.Response.WriteAsJsonAsync(errorRes, cancellationToken: cancellationToken);
        return true;
    }
}
