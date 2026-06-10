// Marketplace.EventGateway/HttpClients/LocalizacionesHttpClient.cs

using System.Text.Json;

namespace Marketplace.EventGateway.HttpClients;

/// <summary>
/// Cliente HTTP hacia MS.Localizaciones.
/// Resuelve consultas síncronas de localizaciones para las
/// queries GraphQL del Marketplace.
/// </summary>
public class LocalizacionesHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalizacionesHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LocalizacionesHttpClient(
        HttpClient httpClient,
        ILogger<LocalizacionesHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<LocalizacionDto>> ObtenerTodasAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/v2/marketplace/localizaciones", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<LocalizacionDto>>>(
                json, JsonOptions);
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalizacionesHttpClient: Error al obtener localizaciones.");
            return [];
        }
    }

    public async Task<LocalizacionDto?> ObtenerPorIdAsync(
        int idLocalizacion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v2/marketplace/localizaciones/{idLocalizacion}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<LocalizacionDto>>(
                json, JsonOptions);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalizacionesHttpClient: Error al obtener localización {Id}.",
                idLocalizacion);
            return null;
        }
    }
}

// ── DTO interno — mapeado exactamente a los campos de LocalizacionResponse ────

public class LocalizacionDto
{
    public int id_localizacion { get; set; }
    public Guid localizacion_guid { get; set; }
    public string codigo_localizacion { get; set; } = string.Empty;
    public string nombre_localizacion { get; set; } = string.Empty;
    public string direccion_localizacion { get; set; } = string.Empty;
    public string telefono_contacto { get; set; } = string.Empty;
    public string correo_contacto { get; set; } = string.Empty;
    public string horario_atencion { get; set; } = string.Empty;
    public string zona_horaria { get; set; } = string.Empty;
    public int id_ciudad { get; set; }
    public string estado_localizacion { get; set; } = string.Empty;
    public bool es_eliminado { get; set; }
}