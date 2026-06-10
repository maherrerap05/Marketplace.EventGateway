// Marketplace.EventGateway/Events/Messages/ReservaSolicitadaEvent.cs

namespace Marketplace.EventGateway.Events.Messages;

/// <summary>
/// Evento publicado por el Gateway hacia RabbitMQ cuando un cliente
/// solicita una reserva desde el Marketplace.
///
/// El Gateway resuelve los IDs de cliente y conductores antes de publicar.
/// MS.Reservas recibe este evento con todos los IDs ya resueltos.
///
/// Routing key: marketplace.reserva.solicitada
/// Cola destino: q.reservas.solicitadas
/// </summary>
public class ReservaSolicitadaEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime FechaEventoUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador de correlación para que el frontend pueda
    /// consultar el estado de la reserva por polling.
    /// </summary>
    public Guid CorrelationId { get; set; }

    public string CodigoReserva { get; set; } = string.Empty;

    /// <summary>ID resuelto por el Gateway via upsert a MS.Clientes.</summary>
    public int IdCliente { get; set; }

    public int IdVehiculo { get; set; }
    public int IdLocalizacionRecogida { get; set; }
    public int IdLocalizacionDevolucion { get; set; }

    /// <summary>Formato: yyyy-MM-dd</summary>
    public string FechaRecogida { get; set; } = string.Empty;

    /// <summary>Formato: HH:mm:ss</summary>
    public string HoraRecogida { get; set; } = string.Empty;

    /// <summary>Formato: yyyy-MM-dd</summary>
    public string FechaDevolucion { get; set; } = string.Empty;

    /// <summary>Formato: HH:mm:ss</summary>
    public string HoraDevolucion { get; set; } = string.Empty;

    public double SubtotalVehiculo { get; set; }
    public double SubtotalExtras { get; set; }
    public double SubtotalReserva { get; set; }
    public double ValorIva { get; set; }
    public double TotalReserva { get; set; }

    public string? Observaciones { get; set; }
    public string OrigenCanalReserva { get; set; } = "MARKETPLACE";

    /// <summary>Conductores con IDs ya resueltos por el Gateway.</summary>
    public List<ConductorEventItem> Conductores { get; set; } = [];

    /// <summary>Extras con valores pre-calculados por el Gateway.</summary>
    public List<ExtraEventItem> Extras { get; set; } = [];

    public string CreadoPorUsuario { get; set; } = "MARKETPLACE-GATEWAY";
    public string ModificacionIp { get; set; } = string.Empty;
    public string ServicioOrigen { get; set; } = "Marketplace.EventGateway";
}

public class ConductorEventItem
{
    /// <summary>ID resuelto por el Gateway via upsert a MS.Clientes.</summary>
    public int IdConductor { get; set; }
    public string TipoConductor { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public string EstadoReservaConductor { get; set; } = "ACT";
}

public class ExtraEventItem
{
    public int IdExtra { get; set; }
    public int Cantidad { get; set; }
    public double ValorUnitario { get; set; }
    public double Subtotal { get; set; }
}