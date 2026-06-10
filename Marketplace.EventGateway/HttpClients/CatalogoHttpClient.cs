// Marketplace.EventGateway/HttpClients/CatalogoHttpClient.cs

using System.Text.Json;

namespace Marketplace.EventGateway.HttpClients;

/// <summary>
/// Cliente HTTP hacia MS.Catálogo.
/// Resuelve consultas síncronas de vehículos, extras y categorías
/// para las queries GraphQL del Marketplace.
/// </summary>
public class CatalogoHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogoHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatalogoHttpClient(HttpClient httpClient, ILogger<CatalogoHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ── Vehículos ─────────────────────────────────────────────────────────

    public async Task<List<VehiculoDto>> ObtenerVehiculosDisponiblesAsync(
        int idLocalizacionRecogida,
        DateTime fechaHoraRecogida,
        DateTime fechaHoraDevolucion,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/marketplace/vehiculos" +
                  $"?id_localizacion_recogida={idLocalizacionRecogida}" +
                  $"&fecha_hora_recogida={fechaHoraRecogida:yyyy-MM-ddTHH:mm:ss}" +
                  $"&fecha_hora_devolucion={fechaHoraDevolucion:yyyy-MM-ddTHH:mm:ss}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<VehiculoDto>>>(json, JsonOptions);
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CatalogoHttpClient: Error al obtener vehículos disponibles. " +
                "Localización: {IdLocalizacion}", idLocalizacionRecogida);
            return [];
        }
    }

    public async Task<VehiculoDto?> ObtenerVehiculoPorIdAsync(
        int idVehiculo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v2/marketplace/vehiculos/{idVehiculo}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<VehiculoDto>>(json, JsonOptions);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CatalogoHttpClient: Error al obtener vehículo {IdVehiculo}.", idVehiculo);
            return null;
        }
    }

    // ── Extras ────────────────────────────────────────────────────────────

    public async Task<List<ExtraDto>> ObtenerExtrasAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/v2/marketplace/extras", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<ExtraDto>>>(json, JsonOptions);
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CatalogoHttpClient: Error al obtener extras.");
            return [];
        }
    }

    public async Task<ExtraDto?> ObtenerExtraPorIdAsync(
        int idExtra,
        CancellationToken cancellationToken = default)
    {
        var extras = await ObtenerExtrasAsync(cancellationToken);
        return extras.FirstOrDefault(e => e.id_extra == idExtra);
    }

    // ── Categorías ────────────────────────────────────────────────────────

    public async Task<List<CategoriaVehiculoDto>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/v2/marketplace/categorias-vehiculo", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<CategoriaVehiculoDto>>>(json, JsonOptions);
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CatalogoHttpClient: Error al obtener categorías.");
            return [];
        }
    }
}

// ── DTOs internos — mapeados exactamente a los campos de MS.Catálogo ──────────

public class VehiculoDto
{
    public int id_vehiculo { get; set; }
    public Guid vehiculo_guid { get; set; }
    public string codigo_interno_vehiculo { get; set; } = string.Empty;
    public string placa_vehiculo { get; set; } = string.Empty;
    public string modelo_vehiculo { get; set; } = string.Empty;
    public short anio_fabricacion { get; set; }
    public string color_vehiculo { get; set; } = string.Empty;
    public string tipo_combustible { get; set; } = string.Empty;
    public string tipo_transmision { get; set; } = string.Empty;
    public short capacidad_pasajeros { get; set; }
    public short capacidad_maletas { get; set; }
    public short numero_puertas { get; set; }
    public int localizacion_actual { get; set; }
    public decimal precio_base_dia { get; set; }
    public int kilometraje_actual { get; set; }
    public string? observaciones_generales { get; set; }
    public string? imagen_referencial_url { get; set; }
    public string estado_vehiculo { get; set; } = string.Empty;
    public bool es_eliminado { get; set; }
    public int id_marca_vehiculo { get; set; }
    public int id_categoria_vehiculo { get; set; }
    public bool aire_acondicionado { get; set; }
}

public class ExtraDto
{
    public int id_extra { get; set; }
    public Guid extra_guid { get; set; }
    public string codigo_extra { get; set; } = string.Empty;
    public string nombre_extra { get; set; } = string.Empty;
    public string descripcion_extra { get; set; } = string.Empty;
    public decimal valor_fijo { get; set; }
    public string estado_extra { get; set; } = string.Empty;
    public bool es_eliminado { get; set; }
}

public class CategoriaVehiculoDto
{
    public int id_categoria_vehiculo { get; set; }
    public Guid categoria_vehiculo_guid { get; set; }
    public string codigo_categoria_vehiculo { get; set; } = string.Empty;
    public string nombre_categoria_vehiculo { get; set; } = string.Empty;
    public string? descripcion_categoria_vehiculo { get; set; }
    public string estado_categoria_vehiculo { get; set; } = string.Empty;
    public bool es_eliminado { get; set; }
}

/// <summary>Wrapper genérico para deserializar las respuestas de los microservicios.</summary>
public class ApiResponse<T>
{
    public int Status { get; set; }
    public string? Mensaje { get; set; }
    public T? Data { get; set; }
}