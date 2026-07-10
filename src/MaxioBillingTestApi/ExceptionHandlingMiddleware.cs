using System.Net;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;

namespace Microsoft.eShopWeb.MaxioBillingTestApi;

/// <summary>
/// Surfaces MaxioBillingClient's typed exceptions as HTTP responses (mirrors PublicApi's
/// ExceptionMiddleware). The client only throws <see cref="SubscriptionNotFoundException"/> and
/// <see cref="BillingProviderException"/> - nothing here reshapes success responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SubscriptionNotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (BillingProviderException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadGateway, ex.Message);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsJsonAsync(new { statusCode = (int)statusCode, message });
    }
}
