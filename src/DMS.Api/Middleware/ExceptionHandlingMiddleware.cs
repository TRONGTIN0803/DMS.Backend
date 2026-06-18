using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception");

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected server error",
                Detail = "An unexpected error occurred while processing the request.",
                Instance = context.Request.Path
            };

            context.Response.StatusCode = problem.Status.Value;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

