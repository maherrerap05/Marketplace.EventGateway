// Marketplace.EventGateway/GraphQL/Queries/MarketplaceQuery.cs

using Marketplace.EventGateway.GraphQL.Types;
using Marketplace.EventGateway.HttpClients;
using Marketplace.EventGateway.Services;

namespace Marketplace.EventGateway.GraphQL.Queries;

/// <summary>
/// Queries síncronas del Marketplace expuestas via GraphQL.
/// Todas las operaciones de lectura pasan por aquí y resuelven
/// sus datos llamando a los microservicios correspondientes.
/// </summary>
public class MarketplaceQuery
{
    // ── Localizaciones ────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todas las localizaciones disponibles del Marketplace.
    /// Equivalente REST: GET /api/v2/marketplace/localizaciones
    /// </summary>
    public async Task<List<LocalizacionType>> ObtenerLocalizaciones(
        [Service] LocalizacionesHttpClient localizacionesClient,
        CancellationToken cancellationToken)
    {
        var dtos = await localizacionesClient.ObtenerTodasAsync(cancellationToken);
        return dtos.Select(LocalizacionType.FromDto).ToList();
    }

    /// <summary>
    /// Devuelve una localización por su ID.
    /// Equivalente REST: GET /api/v2/marketplace/localizaciones/{id}
    /// </summary>
    public async Task<LocalizacionType?> ObtenerLocalizacion(
        int idLocalizacion,
        [Service] LocalizacionesHttpClient localizacionesClient,
        CancellationToken cancellationToken)
    {
        var dto = await localizacionesClient.ObtenerPorIdAsync(
            idLocalizacion, cancellationToken);
        return dto is null ? null : LocalizacionType.FromDto(dto);
    }

    // ── Vehículos ─────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve los vehículos disponibles para una localización y fechas dadas.
    /// Equivalente REST: GET /api/v2/marketplace/vehiculos
    /// </summary>
    public async Task<List<VehiculoType>> ObtenerVehiculosDisponibles(
        int idLocalizacionRecogida,
        string fechaHoraRecogida,
        string fechaHoraDevolucion,
        [Service] CatalogoHttpClient catalogoClient,
        [Service] IHttpClientFactory httpClientFactory,
        [Service] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!DateTime.TryParse(fechaHoraRecogida, out var fechaRecogida))
            throw new ArgumentException(
                $"Formato de fecha inválido para fechaHoraRecogida: {fechaHoraRecogida}");

        if (!DateTime.TryParse(fechaHoraDevolucion, out var fechaDevolucion))
            throw new ArgumentException(
                $"Formato de fecha inválido para fechaHoraDevolucion: {fechaHoraDevolucion}");

        var dtos = await catalogoClient.ObtenerVehiculosDisponiblesAsync(
            idLocalizacionRecogida,
            fechaRecogida,
            fechaDevolucion,
            cancellationToken);

        if (!dtos.Any())
            return [];

        // Verificar disponibilidad real contra MS.Reservas via ESB para cada vehículo.
        var esbBaseUrl = configuration["HttpClients:Esb:BaseUrl"]
            ?? throw new InvalidOperationException("HttpClients:Esb:BaseUrl no configurado.");

        var httpClient = httpClientFactory.CreateClient("Esb");
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var fechaRecogidaZ = fechaRecogida.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var fechaDevolucionZ = fechaDevolucion.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var vehiculosDisponibles = new List<VehiculoType>();

        var tareas = dtos.Select(async dto =>
        {
            try
            {
                var url = $"api/v2/booking/reservas/{dto.id_vehiculo}/disponibilidad" +
                          $"?fechaRecogida={Uri.EscapeDataString(fechaRecogidaZ)}" +
                          $"&fechaDevolucion={Uri.EscapeDataString(fechaDevolucionZ)}" +
                          $"&idLocalizacion={idLocalizacionRecogida}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return (dto, disponible: false);

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var wrapper = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<DisponibilidadResponseDto>>(json, jsonOptions);
                var disponible = wrapper?.Data?.Disponibilidad?.Disponible ?? false;
                return (dto, disponible);
            }
            catch
            {
                return (dto, disponible: false);
            }
        });

        var resultados = await Task.WhenAll(tareas);

        return resultados
            .Where(r => r.disponible)
            .Select(r => VehiculoType.FromDto(r.dto))
            .ToList();
    }

    /// <summary>
    /// Devuelve un vehículo por su ID.
    /// Equivalente REST: GET /api/v2/marketplace/vehiculos/{id}
    /// </summary>
    public async Task<VehiculoType?> ObtenerVehiculo(
        int idVehiculo,
        [Service] CatalogoHttpClient catalogoClient,
        CancellationToken cancellationToken)
    {
        var dto = await catalogoClient.ObtenerVehiculoPorIdAsync(
            idVehiculo, cancellationToken);
        return dto is null ? null : VehiculoType.FromDto(dto);
    }

    // ── Extras ────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los extras disponibles en el Marketplace.
    /// Equivalente REST: GET /api/v2/marketplace/extras
    /// </summary>
    public async Task<List<ExtraType>> ObtenerExtras(
        [Service] CatalogoHttpClient catalogoClient,
        CancellationToken cancellationToken)
    {
        var dtos = await catalogoClient.ObtenerExtrasAsync(cancellationToken);
        return dtos.Select(ExtraType.FromDto).ToList();
    }

    // ── Categorías ────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todas las categorías de vehículos disponibles.
    /// Equivalente REST: GET /api/v2/marketplace/categorias-vehiculo
    /// </summary>
    public async Task<List<CategoriaVehiculoType>> ObtenerCategoriasVehiculo(
        [Service] CatalogoHttpClient catalogoClient,
        CancellationToken cancellationToken)
    {
        var dtos = await catalogoClient.ObtenerCategoriasAsync(cancellationToken);
        return dtos.Select(CategoriaVehiculoType.FromDto).ToList();
    }

    // ── Disponibilidad ────────────────────────────────────────────────────

    /// <summary>
    /// Verifica si un vehículo está disponible para las fechas dadas.
    /// Llama al endpoint del ESB: GET /api/v2/booking/reservas/{id}/disponibilidad
    /// </summary>
    public async Task<DisponibilidadType> VerificarDisponibilidad(
        int idVehiculo,
        string fechaRecogida,
        string fechaDevolucion,
        int idLocalizacion,
        [Service] EsbHttpClient esbClient,
        [Service] IHttpClientFactory httpClientFactory,
        [Service] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            var esbBaseUrl = configuration["HttpClients:Esb:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "HttpClients:Esb:BaseUrl no configurado.");

            var httpClient = httpClientFactory.CreateClient("Esb");

            var url = $"api/v2/booking/reservas/{idVehiculo}/disponibilidad" +
                      $"?fechaRecogida={Uri.EscapeDataString(fechaRecogida)}" +
                      $"&fechaDevolucion={Uri.EscapeDataString(fechaDevolucion)}" +
                      $"&idLocalizacion={idLocalizacion}";

            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            // POR esto:
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var wrapper = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<DisponibilidadResponseDto>>(json, jsonOptions);

            var data = wrapper?.Data;

            return new DisponibilidadType
            {
                IdVehiculo = data?.IdVehiculo ?? idVehiculo,
                IdLocalizacion = data?.IdLocalizacion ?? idLocalizacion,
                Disponible = data?.Disponibilidad?.Disponible ?? false,
                FechaRecogida = data?.Disponibilidad?.FechaRecogida ?? fechaRecogida,
                FechaDevolucion = data?.Disponibilidad?.FechaDevolucion ?? fechaDevolucion
            };
        }
        catch (Exception ex)
        {
            return new DisponibilidadType
            {
                IdVehiculo = idVehiculo,
                IdLocalizacion = idLocalizacion,
                Disponible = false,
                FechaRecogida = fechaRecogida,
                FechaDevolucion = fechaDevolucion
            };
        }
    }

    // ── Búsqueda de conductor por identificación ──────────────────────────

    /// <summary>
    /// Busca un conductor por número de identificación en MS.Clientes.
    /// Usado por la app móvil para autocompletar el formulario de conductor.
    /// Devuelve null si el conductor no existe o está inactivo.
    /// </summary>
    public async Task<ConductorBusquedaType?> BuscarConductorPorIdentificacion(
        string numeroIdentificacion,
        [Service] ClientesHttpClient clientesClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var conductor = await clientesClient
                .BuscarConductorPorIdentificacionAsync(
                    numeroIdentificacion, cancellationToken);

            if (conductor is null)
                return null;

            // Verificar que el conductor esté activo y no eliminado.
            if (conductor.estado_conductor != "ACT" || conductor.es_eliminado)
                return null;

            return new ConductorBusquedaType
            {
                IdConductor = conductor.id_conductor,
                NumeroIdentificacion = conductor.numero_identificacion,
                TipoIdentificacion = conductor.tipo_identificacion,
                ConNombre1 = conductor.con_nombre1,
                ConNombre2 = conductor.con_nombre2,
                ConApellido1 = conductor.con_apellido1,
                ConApellido2 = conductor.con_apellido2,
                NumeroLicencia = conductor.numero_licencia,
                FechaVencimientoLicencia = conductor.fecha_vencimiento_licencia
                    .ToString("yyyy-MM-dd"),
                EdadConductor = conductor.edad_conductor,
                ConTelefono = conductor.con_telefono,
                ConCorreo = conductor.con_correo,
                EstadoConductor = conductor.estado_conductor,
                EsEliminado = conductor.es_eliminado
            };
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    // ── Búsqueda de cliente por correo ────────────────────────────────────

    /// <summary>
    /// Busca un cliente por correo electrónico en MS.Clientes.
    /// Usado por la app móvil para autocompletar el formulario del titular.
    /// Devuelve null si el cliente no existe o está inactivo.
    /// </summary>
    public async Task<ClienteBusquedaType?> BuscarClientePorCorreo(
        string correo,
        [Service] ClientesHttpClient clientesClient,
        [Service] EsbHttpClient esbClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokenJwt = await esbClient.ObtenerTokenInternoAsync(cancellationToken);

            var cliente = await clientesClient.BuscarClientePorCorreoAsync(
                correo, tokenJwt, cancellationToken);

            if (cliente is null)
                return null;

            // Verificar que el cliente esté activo y no eliminado.
            if (cliente.estado != "ACT" || cliente.es_eliminado)
                return null;

            return new ClienteBusquedaType
            {
                IdCliente = cliente.id_cliente,
                TipoIdentificacion = cliente.tipo_identificacion,
                NumeroIdentificacion = cliente.numero_identificacion,
                Nombres = cliente.nombres,
                Apellidos = cliente.apellidos,
                Correo = cliente.correo,
                Telefono = cliente.telefono,
                Estado = cliente.estado,
                EsEliminado = cliente.es_eliminado
            };
        }
        catch
        {
            return null;
        }
    }

    // ── Estado de reserva ─────────────────────────────────────────────────

    /// <summary>
    /// Consulta el estado actual de una reserva por su correlationId.
    /// Usado por el frontend para polling de consistencia eventual.
    /// </summary>
    public EstadoReservaType? ConsultarEstadoReserva(
        Guid correlationId,
        [Service] IEstadoReservaService estadoService)
    {
        var estado = estadoService.Obtener(correlationId);
        if (estado is null) return null;

        return new EstadoReservaType
        {
            CorrelationId = estado.CorrelationId,
            Estado = estado.Estado.ToString(),
            CodigoReserva = estado.CodigoReserva,
            Mensaje = estado.Mensaje,
            CodigoError = estado.CodigoError,
            NumeroFactura = estado.NumeroFactura,
            EstadoFactura = estado.EstadoFactura,
            FechaEmisionFactura = estado.FechaEmisionFactura,
            FechaReservaUtc = estado.FechaReservaUtc,
            CantidadDias = estado.CantidadDias,
            SubtotalVehiculo = estado.SubtotalVehiculo,
            SubtotalExtras = estado.SubtotalExtras,
            Subtotal = estado.Subtotal,
            Iva = estado.Iva,
            Total = estado.Total
        };
    }
}

// ── DTOs internos para deserializar la respuesta de disponibilidad del ESB ────

internal class DisponibilidadResponseDto
{
    public int IdVehiculo { get; set; }
    public int IdLocalizacion { get; set; }
    public DisponibilidadDetalleDto? Disponibilidad { get; set; }
}

internal class DisponibilidadDetalleDto
{
    public string FechaRecogida { get; set; } = string.Empty;
    public string FechaDevolucion { get; set; } = string.Empty;
    public bool Disponible { get; set; }
}