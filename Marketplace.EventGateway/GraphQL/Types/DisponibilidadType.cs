// Marketplace.EventGateway/GraphQL/Types/DisponibilidadType.cs

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa el resultado de verificar
/// la disponibilidad de un vehículo para unas fechas dadas.
/// Mapeado exactamente al contrato del endpoint
/// GET /api/v2/booking/reservas/{id}/disponibilidad del ESB.
/// </summary>
public class DisponibilidadType
{
    public int IdVehiculo { get; set; }
    public int IdLocalizacion { get; set; }
    public bool Disponible { get; set; }
    public string FechaRecogida { get; set; } = string.Empty;
    public string FechaDevolucion { get; set; } = string.Empty;
}