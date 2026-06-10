// Marketplace.EventGateway/GraphQL/Types/ExtraType.cs

using Marketplace.EventGateway.HttpClients;

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa un extra disponible en el Marketplace.
/// </summary>
public class ExtraType
{
    public int IdExtra { get; set; }
    public string CodigoExtra { get; set; } = string.Empty;
    public string NombreExtra { get; set; } = string.Empty;
    public string DescripcionExtra { get; set; } = string.Empty;
    public decimal ValorFijo { get; set; }
    public string EstadoExtra { get; set; } = string.Empty;

    public static ExtraType FromDto(ExtraDto dto) => new()
    {
        IdExtra = dto.id_extra,
        CodigoExtra = dto.codigo_extra,
        NombreExtra = dto.nombre_extra,
        DescripcionExtra = dto.descripcion_extra,
        ValorFijo = dto.valor_fijo,
        EstadoExtra = dto.estado_extra
    };
}