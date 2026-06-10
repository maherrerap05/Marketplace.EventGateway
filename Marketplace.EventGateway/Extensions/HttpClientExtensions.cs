// Marketplace.EventGateway/Extensions/HttpClientExtensions.cs

using Marketplace.EventGateway.HttpClients;

namespace Marketplace.EventGateway.Extensions;

/// <summary>
/// Registra todos los HttpClients tipados del Gateway en el DI container.
/// Cada cliente apunta a su microservicio correspondiente según appsettings.
/// </summary>
public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── MS.Catálogo ───────────────────────────────────────────────────
        services.AddHttpClient<CatalogoHttpClient>(client =>
        {
            var baseUrl = configuration["HttpClients:Catalogo:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "HttpClients:Catalogo:BaseUrl no configurado.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── MS.Localizaciones ─────────────────────────────────────────────
        services.AddHttpClient<LocalizacionesHttpClient>(client =>
        {
            var baseUrl = configuration["HttpClients:Localizaciones:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "HttpClients:Localizaciones:BaseUrl no configurado.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // MS.Localizaciones corre en HTTPS con certificado de desarrollo
            // en local. En producción (Oracle Cloud) corre en HTTP interno.
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // ── MS.Clientes ───────────────────────────────────────────────────
        services.AddHttpClient<ClientesHttpClient>(client =>
        {
            var baseUrl = configuration["HttpClients:Clientes:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "HttpClients:Clientes:BaseUrl no configurado.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── ESB Middleware.RedCar ─────────────────────────────────────────
        services.AddHttpClient<EsbHttpClient>(client =>
        {
            var baseUrl = configuration["HttpClients:Esb:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "HttpClients:Esb:BaseUrl no configurado.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // El ESB también corre en HTTPS con certificado de desarrollo local.
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // ── Cliente genérico para disponibilidad (usado en MarketplaceQuery) ──
        services.AddHttpClient("Esb", client =>
        {
            var baseUrl = configuration["HttpClients:Esb:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "HttpClients:Esb:BaseUrl no configurado.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        return services;
    }
}