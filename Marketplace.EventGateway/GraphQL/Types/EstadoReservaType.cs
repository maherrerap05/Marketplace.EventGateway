// Marketplace.EventGateway/GraphQL/Types/EstadoReservaType.cs

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa el estado actual de una reserva
/// en el flujo asíncrono del Marketplace.
/// Consumido por el frontend mediante polling.
/// </summary>
public class EstadoReservaType
{
    public Guid CorrelationId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string CodigoReserva { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? CodigoError { get; set; }
    public string? NumeroFactura { get; set; }
    public string? EstadoFactura { get; set; }
    public string? FechaEmisionFactura { get; set; }
    public string? FechaReservaUtc { get; set; }
    public int CantidadDias { get; set; }
    public double SubtotalVehiculo { get; set; }
    public double SubtotalExtras { get; set; }
    public double Subtotal { get; set; }
    public double Iva { get; set; }
    public double Total { get; set; }
}