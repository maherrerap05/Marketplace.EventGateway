// Marketplace.EventGateway/GraphQL/Types/ClienteBusquedaType.cs

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa el resultado de buscar un cliente
/// por correo electrónico en MS.Clientes.
/// </summary>
public class ClienteBusquedaType
{
    public int IdCliente { get; set; }
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool EsEliminado { get; set; }
}