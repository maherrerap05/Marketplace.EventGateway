// Marketplace.EventGateway/GraphQL/Types/CategoriaVehiculoType.cs

using Marketplace.EventGateway.HttpClients;

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa una categoría de vehículo.
/// </summary>
public class CategoriaVehiculoType
{
    public int IdCategoriaVehiculo { get; set; }
    public string CodigoCategoriaVehiculo { get; set; } = string.Empty;
    public string NombreCategoriaVehiculo { get; set; } = string.Empty;
    public string? DescripcionCategoriaVehiculo { get; set; }
    public string EstadoCategoriaVehiculo { get; set; } = string.Empty;

    public static CategoriaVehiculoType FromDto(CategoriaVehiculoDto dto) => new()
    {
        IdCategoriaVehiculo = dto.id_categoria_vehiculo,
        CodigoCategoriaVehiculo = dto.codigo_categoria_vehiculo,
        NombreCategoriaVehiculo = dto.nombre_categoria_vehiculo,
        DescripcionCategoriaVehiculo = dto.descripcion_categoria_vehiculo,
        EstadoCategoriaVehiculo = dto.estado_categoria_vehiculo
    };
}