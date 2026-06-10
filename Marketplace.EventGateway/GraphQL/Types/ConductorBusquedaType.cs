// Marketplace.EventGateway/GraphQL/Types/ConductorBusquedaType.cs

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa el resultado de buscar un conductor
/// por número de identificación en MS.Clientes.
/// </summary>
public class ConductorBusquedaType
{
    public int IdConductor { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string ConNombre1 { get; set; } = string.Empty;
    public string? ConNombre2 { get; set; }
    public string ConApellido1 { get; set; } = string.Empty;
    public string? ConApellido2 { get; set; }
    public string NumeroLicencia { get; set; } = string.Empty;
    public string FechaVencimientoLicencia { get; set; } = string.Empty;
    public short EdadConductor { get; set; }
    public string ConTelefono { get; set; } = string.Empty;
    public string ConCorreo { get; set; } = string.Empty;
    public string EstadoConductor { get; set; } = string.Empty;
    public bool EsEliminado { get; set; }
}