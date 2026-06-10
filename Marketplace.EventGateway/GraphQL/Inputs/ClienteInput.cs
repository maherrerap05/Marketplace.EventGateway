// Marketplace.EventGateway/GraphQL/Inputs/ClienteInput.cs

namespace Marketplace.EventGateway.GraphQL.Inputs;

/// <summary>
/// Input GraphQL con los datos crudos del cliente titular de la reserva.
/// El Gateway hará upsert a MS.Clientes para resolver el id_cliente.
/// </summary>
public class ClienteInput
{
    /// <summary>Ej: CED, RUC</summary>
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string? Apellidos { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
}