using System;
using System.Collections.Generic;

namespace Microsoft.eShopWeb.Infrastructure.Services;

// Wire-level DTOs for the Maxio Advanced Billing HTTP API. Property names are translated to/from
// Maxio's snake_case JSON via JsonNamingPolicy.SnakeCaseLower (configured once in MaxioBillingClient).
// These are intentionally minimal - only the fields this integration actually reads or writes.

internal class ProductWire
{
    public long Id { get; set; }
    public string Handle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long PriceInCents { get; set; }
    public int Interval { get; set; }
    public string IntervalUnit { get; set; } = string.Empty;
}

internal class ProductEnvelope
{
    public ProductWire? Product { get; set; }
}

internal class ComponentWire
{
    public long Id { get; set; }
    public string Handle { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

internal class ComponentEnvelope
{
    public ComponentWire? Component { get; set; }
}

internal class SubscriptionComponentWire
{
    public int? UnitBalance { get; set; }
}

internal class SubscriptionComponentEnvelope
{
    public SubscriptionComponentWire? Component { get; set; }
}

internal class CustomerWire
{
    public long Id { get; set; }
    public string? Reference { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

internal class CustomerEnvelope
{
    public CustomerWire? Customer { get; set; }
}

internal class CreateCustomerWire
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

internal class CreateCustomerRequest
{
    public CreateCustomerWire Customer { get; set; } = new();
}

internal class SubscriptionWire
{
    public long Id { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTimeOffset? NextAssessmentAt { get; set; }
    public bool? CancelAtEndOfPeriod { get; set; }
    public string? NextProductHandle { get; set; }
    public ProductWire? Product { get; set; }
    public CustomerWire? Customer { get; set; }
}

internal class SubscriptionEnvelope
{
    public SubscriptionWire? Subscription { get; set; }
}

internal class CreateSubscriptionWire
{
    public string ProductHandle { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string PaymentCollectionMethod { get; set; } = "remittance";
}

internal class CreateSubscriptionRequest
{
    public CreateSubscriptionWire Subscription { get; set; } = new();
}

internal class UsageWire
{
    public long Id { get; set; }
    public int Quantity { get; set; }
    public string? Memo { get; set; }
}

internal class UsageEnvelope
{
    public UsageWire? Usage { get; set; }
}

internal class CreateUsageWire
{
    public int Quantity { get; set; }
    public string? Memo { get; set; }
}

internal class CreateUsageRequest
{
    public CreateUsageWire Usage { get; set; } = new();
}

internal class MigrationOptionsWire
{
    public string ProductHandle { get; set; } = string.Empty;
}

internal class MigrationPreviewRequest
{
    public MigrationOptionsWire Migration { get; set; } = new();
}

internal class MigrationPreviewWire
{
    public long ProratedAdjustmentInCents { get; set; }
    public long ChargeInCents { get; set; }
    public long PaymentDueInCents { get; set; }
    public long CreditAppliedInCents { get; set; }
}

internal class MigrationPreviewEnvelope
{
    public MigrationPreviewWire? Migration { get; set; }
}

internal class UpdateSubscriptionWire
{
    public string? ProductHandle { get; set; }
    public bool? ProductChangeDelayed { get; set; }
}

internal class UpdateSubscriptionRequest
{
    public UpdateSubscriptionWire Subscription { get; set; } = new();
}

internal class CancellationOptionsWire
{
    public string? CancellationMessage { get; set; }
}

internal class CancellationRequest
{
    public CancellationOptionsWire Subscription { get; set; } = new();
}

internal class AutoResumeWire
{
    public DateTimeOffset? AutomaticallyResumeAt { get; set; }
}

internal class HoldRequest
{
    public AutoResumeWire Hold { get; set; } = new();
}
