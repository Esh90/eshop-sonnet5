using System;

namespace Microsoft.eShopWeb.ApplicationCore.Exceptions;

public class SubscriptionConfigurationException : Exception
{
    public SubscriptionConfigurationException(string message) : base(message)
    {
    }
}
