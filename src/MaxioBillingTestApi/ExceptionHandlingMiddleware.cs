using System.Net;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;

namespace Microsoft.eShopWeb.MaxioBillingTestApi;

/// <summary>
/// Surfaces MaxioBillingClient's typed exceptions as HTTP responses (mirrors PublicApi's
/// ExceptionMiddleware) - nothing here reshapes success responses. A BillingProviderException whose
/// StatusCode is a genuine upstream 4xx (not 429) passes that status straight through, since it
/// reflects a well-formed rejection (bad input/state) rather than an upstream fault; 429 and any 5xx
/// (or a fault with no captured status, e.g. a malformed/empty body) surface as 502 Bad Gateway.
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
        catch (CustomerNotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (BillingProviderException ex)
        {
            var statusCode = ex.StatusCode is >= 400 and <= 499 and not 429
                ? (HttpStatusCode)ex.StatusCode.Value
                : HttpStatusCode.BadGateway;
            await WriteAsync(context, statusCode, ex.Message);
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
