using System;

namespace Microsoft.eShopWeb.ApplicationCore.Exceptions;

public class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(string customerReference) : base($"No customer found with reference '{customerReference}'")
    {
    }
}
