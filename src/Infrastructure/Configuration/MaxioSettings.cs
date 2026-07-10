using System;

namespace Microsoft.eShopWeb.Infrastructure.Configuration;

/// <summary>
/// Typed configuration for the Maxio Advanced Billing integration (mirrors <c>CatalogSettings</c> usage).
/// Only <see cref="ApiKey"/> is sensitive; everything else is environment metadata. See plan.md §2.3/§5.
/// </summary>
public class MaxioSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Environment { get; set; } = "US";

    /// <summary>
    /// Explicit override for the outbound base URL. When set, it wins over the Subdomain-derived host,
    /// so the same build can target production, a dev/sandbox tenant, or a local mock server.
    /// </summary>
    public string? BaseUrl { get; set; }

    public string ProductFamilyHandle { get; set; } = string.Empty;
    public long ProductFamilyId { get; set; }

    public string DefaultProductHandle { get; set; } = string.Empty;
    public long DefaultProductId { get; set; }

    public string AlternateProductHandle { get; set; } = string.Empty;
    public long AlternateProductId { get; set; }

    public string MeteredComponentHandle { get; set; } = string.Empty;
    public long MeteredComponentId { get; set; }

    /// <summary>
    /// Resolves the base URL to target: the explicit override if present, otherwise the host derived
    /// from <see cref="Subdomain"/> (+ region). This is the one place retargeting (prod/dev/mock) happens.
    /// </summary>
    public string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl.TrimEnd('/');
        }

        return string.Equals(Environment, "EU", StringComparison.OrdinalIgnoreCase)
            ? $"https://{Subdomain}.ebilling.maxio.com"
            : $"https://{Subdomain}.chargify.com";
    }
}
