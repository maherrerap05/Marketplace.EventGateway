// Marketplace.EventGateway/GraphQL/Inputs/CrearReservaInput.cs

namespace Marketplace.EventGateway.GraphQL.Inputs;

/// <summary>
/// Input principal de la mutation crearReserva.
/// Contiene todos los datos necesarios para que el Gateway
/// resuelva los IDs, calcule los totales y publique el evento.
/// </summary>
public class CrearReservaInput
{
    // ── Vehículo y localizaciones ─────────────────────────────────────────

    public int IdVehiculo { get; set; }
    public int IdLocalizacionRecogida { get; set; }
    public int IdLocalizacionDevolucion { get; set; }

    // ── Fechas y horas ────────────────────────────────────────────────────

    /// <summary>Formato: yyyy-MM-dd</summary>
    public string FechaRecogida { get; set; } = string.Empty;

    /// <summary>Formato: HH:mm:ss</summary>
    public string HoraRecogida { get; set; } = string.Empty;

    /// <summary>Formato: yyyy-MM-dd</summary>
    public string FechaDevolucion { get; set; } = string.Empty;

    /// <summary>Formato: HH:mm:ss</summary>
    public string HoraDevolucion { get; set; } = string.Empty;

    // ── Datos del cliente ─────────────────────────────────────────────────

    /// <summary>
    /// Datos crudos del cliente. El Gateway resolverá el id_cliente
    /// mediante upsert a MS.Clientes antes de publicar el evento.
    /// </summary>
    public ClienteInput Cliente { get; set; } = new();

    // ── Conductores ───────────────────────────────────────────────────────

    /// <summary>
    /// Lista de conductores autorizados. El primer elemento
    /// siempre es el conductor principal (EsPrincipal = true).
    /// </summary>
    public List<ConductorInput> Conductores { get; set; } = [];

    // ── Extras ────────────────────────────────────────────────────────────

    /// <summary>
    /// Extras seleccionados. El Gateway consultará el precio unitario
    /// a MS.Catálogo para calcular los subtotales.
    /// </summary>
    public List<ExtraInput> Extras { get; set; } = [];

    // ── Opcionales ────────────────────────────────────────────────────────

    public string? Observaciones { get; set; }
}