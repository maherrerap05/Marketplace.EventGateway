// Marketplace.EventGateway/GraphQL/Types/ReservaResultadoType.cs

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL devuelto inmediatamente por la mutation crearReserva.
/// Contiene el correlationId para que el frontend pueda hacer polling
/// y el código de reserva pre-generado por el Gateway.
/// El estado siempre será "EN_PROCESO" en este momento.
/// </summary>
public class ReservaResultadoType
{
    public Guid CorrelationId { get; set; }
    public string CodigoReserva { get; set; } = string.Empty;
    public string Estado { get; set; } = "EN_PROCESO";
    public string Mensaje { get; set; } = string.Empty;
}