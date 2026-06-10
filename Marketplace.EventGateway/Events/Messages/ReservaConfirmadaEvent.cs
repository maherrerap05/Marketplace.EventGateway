// Marketplace.EventGateway/Events/Messages/ReservaConfirmadaEvent.cs

namespace Marketplace.EventGateway.Events.Messages;

/// <summary>
/// Evento consumido por el Gateway desde la cola q.reservas.confirmadas.
/// Publicado por MS.Reservas cuando la transacción atómica concluye.
/// El Gateway actualiza el estado en memoria para que el frontend
/// pueda consultarlo por polling.
///
/// Routing key de suscripción: marketplace.reserva.confirmada
/// Cola destino: q.reservas.confirmadas
/// </summary>
public class ReservaConfirmadaEvent
{
    public Guid EventId { get; set; }
    public DateTime FechaEventoUtc { get; set; }

    /// <summary>
    /// Mismo CorrelationId del ReservaSolicitadaEvent original.
    /// Permite al Gateway correlacionar solicitud con respuesta.
    /// </summary>
    public Guid CorrelationId { get; set; }

    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string CodigoError { get; set; } = string.Empty;

    public string CodigoReserva { get; set; } = string.Empty;
    public string EstadoReserva { get; set; } = string.Empty;
    public string FechaReservaUtc { get; set; } = string.Empty;
    public int CantidadDias { get; set; }

    public int IdVehiculo { get; set; }
    public int IdCliente { get; set; }

    public string NumeroFactura { get; set; } = string.Empty;
    public string EstadoFactura { get; set; } = string.Empty;
    public string FechaEmisionFactura { get; set; } = string.Empty;

    public double SubtotalVehiculo { get; set; }
    public double SubtotalExtras { get; set; }
    public double Subtotal { get; set; }
    public double Iva { get; set; }
    public double Total { get; set; }
}