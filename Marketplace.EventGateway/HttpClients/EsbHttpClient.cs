// Marketplace.EventGateway/HttpClients/EsbHttpClient.cs

using System.Text.Json;

namespace Marketplace.EventGateway.HttpClients;

/// <summary>
/// Cliente HTTP hacia el ESB Middleware.RedCar.
/// Responsabilidad única en el Gateway: obtener el token JWT interno
/// mediante el endpoint /diagnostico/token-interno, el cual permite
/// al Gateway llamar a endpoints protegidos de MS.Clientes sin
/// exponer credenciales de administrador al frontend.
/// 
/// Este patrón reutiliza el mecanismo existente del ESB sin modificarlo.
/// </summary>
public class EsbHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EsbHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Cache del token para evitar llamadas repetidas al ESB.
    // El token tiene duración de 60 minutos según JwtSettings.
    private string? _tokenCache;
    private DateTime _tokenExpiracion = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public EsbHttpClient(
        HttpClient httpClient,
        ILogger<EsbHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el token JWT interno del ESB.
    /// Usa caché local para no llamar al ESB en cada operación.
    /// El token se renueva automáticamente 5 minutos antes de expirar.
    /// </summary>
    public async Task<string> ObtenerTokenInternoAsync(
        CancellationToken cancellationToken = default)
    {
        // Verificar cache fuera del semáforo para evitar bloqueos innecesarios.
        if (_tokenCache is not null && DateTime.UtcNow < _tokenExpiracion)
            return _tokenCache;

        // Adquirir semáforo para evitar múltiples llamadas concurrentes al ESB.
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check dentro del semáforo.
            if (_tokenCache is not null && DateTime.UtcNow < _tokenExpiracion)
                return _tokenCache;

            _logger.LogInformation(
                "EsbHttpClient: Obteniendo token interno del ESB.");

            var response = await _httpClient.GetAsync(
                "api/v2/diagnostico/token-interno", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var resultado = JsonSerializer.Deserialize<TokenInternoResponse>(
                json, JsonOptions);

            if (string.IsNullOrWhiteSpace(resultado?.Token))
                throw new InvalidOperationException(
                    "El ESB no retornó un token interno válido.");

            _tokenCache = resultado.Token;

            // Usar la expiración real del ESB con margen de 5 minutos.
            _tokenExpiracion = resultado.ExpirationUtc != default
                ? resultado.ExpirationUtc.AddMinutes(-5)
                : DateTime.UtcNow.AddMinutes(55);

            _logger.LogInformation(
                "EsbHttpClient: Token interno obtenido. " +
                "Válido hasta: {Expiracion:HH:mm:ss UTC}", _tokenExpiracion);

            return _tokenCache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "EsbHttpClient: Error al obtener token interno del ESB.");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

// ── DTO interno ───────────────────────────────────────────────────────────────

public class TokenInternoResponse
{
    public string? Token { get; set; }
    public DateTime ExpirationUtc { get; set; }
    public bool EstaVigente { get; set; }
}