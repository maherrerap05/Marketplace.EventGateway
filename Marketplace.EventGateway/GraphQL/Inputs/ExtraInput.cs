// Marketplace.EventGateway/GraphQL/Inputs/ExtraInput.cs

namespace Marketplace.EventGateway.GraphQL.Inputs;

/// <summary>
/// Input GraphQL para un extra seleccionado por el cliente.
/// El Gateway consultará el precio unitario a MS.Catálogo
/// para calcular el subtotal.
/// </summary>
public class ExtraInput
{
    public int IdExtra { get; set; }
    public int Cantidad { get; set; }
}