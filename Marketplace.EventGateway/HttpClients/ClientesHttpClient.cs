// Marketplace.EventGateway/HttpClients/ClientesHttpClient.cs

using System.Text.Json;

namespace Marketplace.EventGateway.HttpClients;

/// <summary>
/// Cliente HTTP hacia MS.Clientes.
/// Responsabilidad en el Gateway: resolver IDs de cliente y conductores
/// mediante upsert antes de publicar el evento a RabbitMQ.
/// 
/// Usa los endpoints públicos del Marketplace para crear y buscar,
/// y los endpoints internos con token JWT para buscar por correo
/// e identificación (obtenido del ESB via EsbHttpClient).
/// </summary>
public class ClientesHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientesHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ClientesHttpClient(
        HttpClient httpClient,
        ILogger<ClientesHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ── Cliente ───────────────────────────────────────────────────────────

    /// <summary>
    /// Busca un cliente por correo usando token JWT interno.
    /// Devuelve null si no existe.
    /// </summary>
    public async Task<ClienteDto?> BuscarClientePorCorreoAsync(
        string correo,
        string tokenJwt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v2/marketplace/clientes/correo/{Uri.EscapeDataString(correo)}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<ClienteDto>>(json, JsonOptions);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ClientesHttpClient: Error al buscar cliente por correo {Correo}. " +
                "Se intentará creación directa.", correo);
            return null;
        }
    }

    /// <summary>
    /// Crea un nuevo cliente usando el endpoint público del Marketplace.
    /// </summary>
    public async Task<ClienteDto> CrearClienteAsync(
        CrearClienteRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            tipo_identificacion = request.TipoIdentificacion,
            numero_identificacion = request.NumeroIdentificacion,
            razon_social = request.RazonSocial,
            nombres = request.Nombres,
            apellidos = request.Apellidos,
            correo = request.Correo,
            telefono = request.Telefono,
            direccion = request.Direccion,
            estado = "ACT",
            creado_por_usuario = "MARKETPLACE-GATEWAY",
            servicio_origen = "MARKETPLACE"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "api/v2/marketplace/clientes", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var wrapper = JsonSerializer.Deserialize<ApiResponse<ClienteDto>>(json, JsonOptions);

        if (wrapper?.Data is null || wrapper.Data.id_cliente == 0)
            throw new InvalidOperationException(
                $"MS.Clientes no retornó id_cliente al crear cliente {request.Correo}.");

        _logger.LogInformation(
            "ClientesHttpClient: Cliente creado. Id: {IdCliente}",
            wrapper.Data.id_cliente);

        return wrapper.Data;
    }

    /// <summary>
    /// Upsert de cliente: busca por correo y crea si no existe.
    /// Devuelve el id_cliente resuelto.
    /// </summary>
    public async Task<int> UpsertClienteAsync(
        CrearClienteRequest request,
        string tokenJwt,
        CancellationToken cancellationToken = default)
    {
        var clienteExistente = await BuscarClientePorCorreoAsync(
            request.Correo, tokenJwt, cancellationToken);

        if (clienteExistente?.id_cliente > 0)
        {
            _logger.LogInformation(
                "ClientesHttpClient: Cliente existente encontrado. Id: {IdCliente}",
                clienteExistente.id_cliente);
            return clienteExistente.id_cliente;
        }

        var clienteCreado = await CrearClienteAsync(request, cancellationToken);
        return clienteCreado.id_cliente;
    }

    // ── Conductor ─────────────────────────────────────────────────────────

    /// <summary>
    /// Busca un conductor por número de identificación usando endpoint público.
    /// Devuelve null si no existe.
    /// </summary>
    public async Task<ConductorDto?> BuscarConductorPorIdentificacionAsync(
        string numeroIdentificacion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v2/marketplace/conductores/identificacion/" +
                $"{Uri.EscapeDataString(numeroIdentificacion)}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<ApiResponse<ConductorDto>>(
                json, JsonOptions);
            return wrapper?.Data;
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ClientesHttpClient: Error al buscar conductor {Identificacion}. " +
                "Se intentará creación directa.", numeroIdentificacion);
            return null;
        }
    }

    /// <summary>
    /// Crea un nuevo conductor usando el endpoint público del Marketplace.
    /// </summary>
    public async Task<ConductorDto> CrearConductorAsync(
        CrearConductorRequest request,
        CancellationToken cancellationToken = default)
    {
        var numeroLicencia = string.IsNullOrWhiteSpace(request.NumeroLicencia)
            ? request.NumeroIdentificacion
            : request.NumeroLicencia;

        var payload = new
        {
            codigo_conductor = $"CON-{request.NumeroIdentificacion}",
            tipo_identificacion = request.TipoIdentificacion,
            numero_identificacion = request.NumeroIdentificacion,
            con_nombre1 = request.ConNombre1,
            con_nombre2 = request.ConNombre2,
            con_apellido1 = request.ConApellido1,
            con_apellido2 = request.ConApellido2,
            numero_licencia = numeroLicencia,
            fecha_vencimiento_licencia = request.FechaVencimientoLicencia,
            edad_conductor = request.EdadConductor,
            con_telefono = request.ConTelefono,
            con_correo = request.ConCorreo,
            estado_conductor = "ACT",
            creado_por_usuario = "MARKETPLACE-GATEWAY",
            origen_registro = "MARKETPLACE"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "api/v2/marketplace/conductores", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var wrapper = JsonSerializer.Deserialize<ApiResponse<ConductorDto>>(
            json, JsonOptions);

        if (wrapper?.Data is null || wrapper.Data.id_conductor == 0)
            throw new InvalidOperationException(
                $"MS.Clientes no retornó id_conductor al crear conductor " +
                $"{request.NumeroIdentificacion}.");

        _logger.LogInformation(
            "ClientesHttpClient: Conductor creado. Id: {IdConductor}",
            wrapper.Data.id_conductor);

        return wrapper.Data;
    }

    /// <summary>
    /// Upsert de conductor: busca por identificación y crea si no existe.
    /// Devuelve el id_conductor resuelto.
    /// </summary>
    public async Task<int> UpsertConductorAsync(
        CrearConductorRequest request,
        CancellationToken cancellationToken = default)
    {
        var conductorExistente = await BuscarConductorPorIdentificacionAsync(
            request.NumeroIdentificacion, cancellationToken);

        if (conductorExistente?.id_conductor > 0)
        {
            _logger.LogInformation(
                "ClientesHttpClient: Conductor existente encontrado. Id: {IdConductor}",
                conductorExistente.id_conductor);
            return conductorExistente.id_conductor;
        }

        var conductorCreado = await CrearConductorAsync(request, cancellationToken);
        return conductorCreado.id_conductor;
    }
}

// ── DTOs internos ─────────────────────────────────────────────────────────────

public class ClienteDto
{
    public int id_cliente { get; set; }
    public Guid cliente_guid { get; set; }
    public string tipo_identificacion { get; set; } = string.Empty;
    public string numero_identificacion { get; set; } = string.Empty;
    public string nombres { get; set; } = string.Empty;
    public string? apellidos { get; set; }
    public string correo { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string estado { get; set; } = string.Empty;
    public bool es_eliminado { get; set; }
}

public class ConductorDto
{
    public int id_conductor { get; set; }
    public Guid conductor_guid { get; set; }
    public string tipo_identificacion { get; set; } = string.Empty;
    public string numero_identificacion { get; set; } = string.Empty;
    public string con_nombre1 { get; set; } = string.Empty;
    public string? con_nombre2 { get; set; }
    public string con_apellido1 { get; set; } = string.Empty;
    public string? con_apellido2 { get; set; }
    public string numero_licencia { get; set; } = string.Empty;
    public DateTime fecha_vencimiento_licencia { get; set; }
    public short edad_conductor { get; set; }
    public string estado_conductor { get; set; } = string.Empty;
    public bool es_eliminado { get; set; }
    public string con_telefono { get; set; } = string.Empty;
    public string con_correo { get; set; } = string.Empty;
}

// ── Requests internos del Gateway ─────────────────────────────────────────────

public class CrearClienteRequest
{
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
}

public class CrearConductorRequest
{
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string ConNombre1 { get; set; } = string.Empty;
    public string? ConNombre2 { get; set; }
    public string ConApellido1 { get; set; } = string.Empty;
    public string? ConApellido2 { get; set; }
    public string? NumeroLicencia { get; set; }
    public string FechaVencimientoLicencia { get; set; } = string.Empty;
    public short EdadConductor { get; set; }
    public string ConTelefono { get; set; } = string.Empty;
    public string ConCorreo { get; set; } = string.Empty;
}