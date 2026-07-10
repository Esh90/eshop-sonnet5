using System;

namespace Microsoft.eShopWeb.ApplicationCore.Exceptions;

public class StalePlanChangePreviewException : Exception
{
    public StalePlanChangePreviewException(string message) : base(message)
    {
    }
}
