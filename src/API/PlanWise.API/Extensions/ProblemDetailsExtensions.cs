using Microsoft.AspNetCore.Diagnostics;

namespace PlanWise.Api.Extensions;

internal static class ProblemDetailsExtensions
{
    internal static void UseProblemDetailsExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
        {
            IProblemDetailsService problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
            Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            int statusCode = exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;

            context.Response.StatusCode = statusCode;

            bool isBadRequest = statusCode == StatusCodes.Status400BadRequest;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = statusCode,
                    Title = isBadRequest ? "Request.InvalidBody" : "Server failure",
                    Detail = isBadRequest
                        ? "The request body could not be parsed. Check that all fields match the expected types and formats."
                        : "An unexpected error occurred"
                }
            });
        }));
    }
}
