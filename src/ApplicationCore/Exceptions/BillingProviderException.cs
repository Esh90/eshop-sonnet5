using System;

namespace Microsoft.eShopWeb.ApplicationCore.Exceptions;

public class BillingProviderException : Exception
{
    /// <summary>The upstream Maxio HTTP status code, when the failure came from an actual response (vs. a network/parse fault).</summary>
    public int? StatusCode { get; }

    public BillingProviderException(string message) : base(message)
    {
    }

    public BillingProviderException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public BillingProviderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
