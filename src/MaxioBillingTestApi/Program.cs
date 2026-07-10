using System.Net.Http.Headers;
using System.Text;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Configuration;
using Microsoft.eShopWeb.Infrastructure.Services;
using Microsoft.eShopWeb.MaxioBillingTestApi;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<MaxioSettings>(builder.Configuration.GetSection("Maxio"));

// Typed client via IHttpClientFactory, wired exactly like the repo's other hosts (Web/PublicApi):
// BaseAddress is resolved from MaxioSettings.ResolveBaseUrl() - an explicit Maxio:BaseUrl always wins,
// which is how appsettings.json here points every outbound call at the mock on http://localhost:8080.
builder.Services.AddHttpClient<IBillingClient, MaxioBillingClient>((sp, http) =>
{
    var settings = sp.GetRequiredService<IOptions<MaxioSettings>>().Value;
    http.BaseAddress = new Uri(settings.ResolveBaseUrl() + "/");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.ApiKey}:x")));
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program { }
