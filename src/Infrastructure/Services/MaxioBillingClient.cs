using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Models;
using Microsoft.eShopWeb.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.Infrastructure.Services;

/// <summary>
/// The single integration point with Maxio Advanced Billing. Talks to Maxio over plain HTTP
/// (no SDK) via a typed <see cref="HttpClient"/> whose BaseAddress/auth are configured in the
/// composition root from <see cref="MaxioSettings"/>. Nothing else in the solution talks to Maxio.
/// </summary>
public class MaxioBillingClient : IBillingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly MaxioSettings _settings;

    public MaxioBillingClient(HttpClient httpClient, IOptions<MaxioSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<IReadOnlyList<BillingPlan>> ListPlansAsync()
    {
        var envelopes = await GetAsync<List<ProductEnvelope>>($"product_families/{_settings.ProductFamilyId}/products.json");
        return (envelopes ?? new List<ProductEnvelope>())
            .Where(e => e.Product != null)
            .Select(e => MapPlan(e.Product!))
            .ToList();
    }

    public async Task<BillingComponent> GetMeteredComponentAsync()
    {
        var envelope = await GetAsync<ComponentEnvelope>($"components/lookup.json?handle={Uri.EscapeDataString(_settings.MeteredComponentHandle)}");
        var component = envelope?.Component
            ?? throw new BillingProviderException($"Metered component '{_settings.MeteredComponentHandle}' could not be found on Maxio.");

        return new BillingComponent
        {
            ComponentId = component.Id,
            Handle = component.Handle,
            Kind = component.Kind
        };
    }

    public async Task<long> EnsureCustomerAsync(string customerReference, string email, string firstName, string lastName)
    {
        var existing = await TryGetAsync<CustomerEnvelope>($"customers/lookup.json?reference={Uri.EscapeDataString(customerReference)}");
        if (existing?.Customer != null)
        {
            return existing.Customer.Id;
        }

        var request = new CreateCustomerRequest
        {
            Customer = new CreateCustomerWire
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Reference = customerReference
            }
        };

        var created = await PostAsync<CustomerEnvelope>("customers.json", request);
        return created?.Customer?.Id
            ?? throw new BillingProviderException("Maxio did not return a customer id after creating the customer.");
    }

    public async Task<BillingSubscription> CreateSubscriptionAsync(long customerId, string productHandle)
    {
        var request = new CreateSubscriptionRequest
        {
            Subscription = new CreateSubscriptionWire { ProductHandle = productHandle, CustomerId = customerId }
        };

        var response = await PostAsync<SubscriptionEnvelope>("subscriptions.json", request);
        return MapSubscription(response?.Subscription
            ?? throw new BillingProviderException("Maxio did not return a subscription after enrollment."));
    }

    public async Task<IReadOnlyList<BillingSubscription>> ListSubscriptionsForCustomerAsync(string customerReference)
    {
        // Mirrors Maxio's own id-or-reference convention (seen elsewhere, e.g. product family id-or-handle):
        // a numeric value is the real Maxio customer id, used directly; anything else is resolved by reference.
        long customerId;
        if (!long.TryParse(customerReference, out customerId))
        {
            var customer = await TryGetAsync<CustomerEnvelope>($"customers/lookup.json?reference={Uri.EscapeDataString(customerReference)}");
            if (customer?.Customer == null)
            {
                throw new CustomerNotFoundException(customerReference);
            }

            customerId = customer.Customer.Id;
        }

        var envelopes = await GetAsync<List<SubscriptionEnvelope>>($"customers/{customerId}/subscriptions.json");
        return (envelopes ?? new List<SubscriptionEnvelope>())
            .Where(e => e.Subscription != null)
            .Select(e => MapSubscription(e.Subscription!))
            .ToList();
    }

    public async Task<BillingSubscription> GetSubscriptionAsync(long subscriptionId)
    {
        var envelope = await TryGetAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}.json");
        if (envelope?.Subscription == null)
        {
            throw new SubscriptionNotFoundException(subscriptionId);
        }

        return MapSubscription(envelope.Subscription);
    }

    public async Task<UsageRecordResult> RecordUsageAsync(long subscriptionId, int quantity, string? memo)
    {
        var request = new CreateUsageRequest { Usage = new CreateUsageWire { Quantity = quantity, Memo = memo } };
        var response = await PostAsync<UsageEnvelope>(
            $"subscriptions/{subscriptionId}/components/{_settings.MeteredComponentId}/usages.json", request);
        var usage = response?.Usage
            ?? throw new BillingProviderException("Maxio did not return a usage record after recording usage.");

        return new UsageRecordResult { SubscriptionId = subscriptionId, QuantityRecorded = usage.Quantity };
    }

    public async Task<int?> GetComponentPeriodUsageAsync(long subscriptionId)
    {
        var envelope = await GetAsync<SubscriptionComponentEnvelope>(
            $"subscriptions/{subscriptionId}/components/{_settings.MeteredComponentId}.json");
        return envelope?.Component?.UnitBalance;
    }

    public async Task<PlanChangePreview> PreviewPlanChangeAsync(long subscriptionId, string targetProductHandle)
    {
        var request = new MigrationPreviewRequest { Migration = new MigrationOptionsWire { ProductHandle = targetProductHandle } };
        var response = await PostAsync<MigrationPreviewEnvelope>($"subscriptions/{subscriptionId}/migrations/preview.json", request);
        var preview = response?.Migration
            ?? throw new BillingProviderException("Maxio did not return a migration preview.");

        var targetProductEnvelope = await GetAsync<ProductEnvelope>($"products/handle/{Uri.EscapeDataString(targetProductHandle)}.json");
        var targetProduct = targetProductEnvelope?.Product;

        return new PlanChangePreview
        {
            SubscriptionId = subscriptionId,
            TargetProductHandle = targetProductHandle,
            TargetProductName = targetProduct?.Name ?? string.Empty,
            TargetPrice = (targetProduct?.PriceInCents ?? 0) / 100m,
            ProratedAdjustmentInCents = preview.ProratedAdjustmentInCents,
            ChargeInCents = preview.ChargeInCents,
            PaymentDueInCents = preview.PaymentDueInCents,
            CreditAppliedInCents = preview.CreditAppliedInCents
        };
    }

    public async Task<BillingSubscription> CommitPlanChangeAsync(long subscriptionId, string targetProductHandle, bool atRenewal)
    {
        if (atRenewal)
        {
            var updateRequest = new UpdateSubscriptionRequest
            {
                Subscription = new UpdateSubscriptionWire { ProductHandle = targetProductHandle, ProductChangeDelayed = true }
            };
            await PutAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}.json", updateRequest);
            return await GetSubscriptionAsync(subscriptionId);
        }

        var migrationRequest = new MigrationPreviewRequest { Migration = new MigrationOptionsWire { ProductHandle = targetProductHandle } };
        var response = await PostAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}/migrations.json", migrationRequest);
        return MapSubscription(response?.Subscription
            ?? throw new BillingProviderException("Maxio did not return a subscription after the plan migration."));
    }

    public async Task<BillingSubscription> PauseSubscriptionAsync(long subscriptionId)
    {
        var response = await PostAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}/hold.json", new HoldRequest());
        return MapSubscription(response?.Subscription
            ?? throw new BillingProviderException("Maxio did not return a subscription after pausing."));
    }

    public async Task<BillingSubscription> ResumeSubscriptionAsync(long subscriptionId)
    {
        var response = await PostAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}/resume.json", null);
        return MapSubscription(response?.Subscription
            ?? throw new BillingProviderException("Maxio did not return a subscription after resuming."));
    }

    public async Task<BillingSubscription> CancelSubscriptionAsync(long subscriptionId, bool endOfPeriod, string? reason)
    {
        var request = new CancellationRequest { Subscription = new CancellationOptionsWire { CancellationMessage = reason } };

        if (endOfPeriod)
        {
            await PostAsync<object>($"subscriptions/{subscriptionId}/delayed_cancel.json", request);
            return await GetSubscriptionAsync(subscriptionId);
        }

        var response = await DeleteAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}.json", request);
        return MapSubscription(response?.Subscription
            ?? throw new BillingProviderException("Maxio did not return a subscription after cancellation."));
    }

    public async Task<BillingSubscription> ReactivateSubscriptionAsync(long subscriptionId)
    {
        var response = await PutAsync<SubscriptionEnvelope>($"subscriptions/{subscriptionId}/reactivate.json", null);
        return MapSubscription(response?.Subscription
            ?? throw new BillingProviderException("Maxio did not return a subscription after reactivation."));
    }

    private static BillingPlan MapPlan(ProductWire product) => new()
    {
        ProductId = product.Id,
        Handle = product.Handle,
        Name = product.Name,
        Price = product.PriceInCents / 100m,
        Interval = product.Interval,
        IntervalUnit = product.IntervalUnit
    };

    private static BillingSubscription MapSubscription(SubscriptionWire subscription) => new()
    {
        SubscriptionId = subscription.Id,
        CustomerReference = subscription.Customer?.Reference ?? string.Empty,
        ProductHandle = subscription.Product?.Handle ?? string.Empty,
        ProductName = subscription.Product?.Name ?? string.Empty,
        Price = (subscription.Product?.PriceInCents ?? 0) / 100m,
        State = subscription.State,
        NextAssessmentAt = subscription.NextAssessmentAt,
        CancelAtEndOfPeriod = subscription.CancelAtEndOfPeriod ?? false,
        PendingProductHandle = subscription.NextProductHandle
    };

    private Task<T?> GetAsync<T>(string relativePath) => SendAsync<T>(HttpMethod.Get, relativePath, null, throwOn404: true);

    private Task<T?> TryGetAsync<T>(string relativePath) => SendAsync<T>(HttpMethod.Get, relativePath, null, throwOn404: false);

    private Task<T?> PostAsync<T>(string relativePath, object? body) => SendAsync<T>(HttpMethod.Post, relativePath, body, throwOn404: true);

    private Task<T?> PutAsync<T>(string relativePath, object? body) => SendAsync<T>(HttpMethod.Put, relativePath, body, throwOn404: true);

    private Task<T?> DeleteAsync<T>(string relativePath, object? body) => SendAsync<T>(HttpMethod.Delete, relativePath, body, throwOn404: true);

    private async Task<T?> SendAsync<T>(HttpMethod method, string relativePath, object? body, bool throwOn404)
    {
        using var request = new HttpRequestMessage(method, relativePath);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }

        using var response = await _httpClient.SendAsync(request);

        if (!throwOn404 && response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new BillingProviderException(
                $"Maxio request failed ({(int)response.StatusCode} {method} {relativePath}): {ExtractErrorMessage(responseBody)}",
                (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new BillingProviderException($"Maxio returned {(int)response.StatusCode} {method} {relativePath} with an empty body.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
        }
        catch (JsonException)
        {
            throw new BillingProviderException($"Maxio returned {(int)response.StatusCode} {method} {relativePath} with an unparseable body.");
        }
    }

    private static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "(no response body)";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors))
            {
                if (errors.ValueKind == JsonValueKind.Array)
                {
                    return string.Join("; ", errors.EnumerateArray().Select(e => e.ToString()));
                }

                if (errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            messages.AddRange(property.Value.EnumerateArray().Select(item => $"{property.Name}: {item}"));
                        }
                        else
                        {
                            messages.Add($"{property.Name}: {property.Value}");
                        }
                    }

                    return string.Join("; ", messages);
                }
            }

            if (root.TryGetProperty("error", out var error))
            {
                return error.ToString();
            }
        }
        catch (JsonException)
        {
            // Not a JSON body (or an unrecognized shape) - fall through and surface it raw.
        }

        return body;
    }
}
