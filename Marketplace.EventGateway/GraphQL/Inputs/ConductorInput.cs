// Marketplace.EventGateway/GraphQL/Inputs/ConductorInput.cs

namespace Marketplace.EventGateway.GraphQL.Inputs;

/// <summary>
/// Input GraphQL con los datos crudos de un conductor autorizado.
/// El Gateway hará upsert a MS.Clientes para resolver el id_conductor.
/// </summary>
public class ConductorInput
{
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string ConNombre1 { get; set; } = string.Empty;
    public string? ConNombre2 { get; set; }
    public string ConApellido1 { get; set; } = string.Empty;
    public string? ConApellido2 { get; set; }

    /// <summary>
    /// Si viene vacío, el Gateway asignará numero_identificacion como licencia.
    /// Comportamiento idéntico al ConductoresBookingController existente.
    /// </summary>
    public string? NumeroLicencia { get; set; }

    /// <summary>Formato ISO 8601: yyyy-MM-dd</summary>
    public string FechaVencimientoLicencia { get; set; } = string.Empty;

    public short EdadConductor { get; set; }
    public string ConTelefono { get; set; } = string.Empty;
    public string ConCorreo { get; set; } = string.Empty;

    /// <summary>true = conductor principal de la reserva.</summary>
    public bool EsPrincipal { get; set; }
}