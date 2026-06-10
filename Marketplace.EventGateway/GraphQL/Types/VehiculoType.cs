// Marketplace.EventGateway/GraphQL/Types/VehiculoType.cs

using Marketplace.EventGateway.HttpClients;

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa un vehículo disponible en el Marketplace.
/// </summary>
public class VehiculoType
{
    public int IdVehiculo { get; set; }
    public string CodigoInternoVehiculo { get; set; } = string.Empty;
    public string ModeloVehiculo { get; set; } = string.Empty;
    public short AniofabricacIon { get; set; }
    public string ColorVehiculo { get; set; } = string.Empty;
    public string TipoCombustible { get; set; } = string.Empty;
    public string TipoTransmision { get; set; } = string.Empty;
    public short CapacidadPasajeros { get; set; }
    public short CapacidadMaletas { get; set; }
    public short NumeroPuertas { get; set; }
    public bool AireAcondicionado { get; set; }
    public decimal PrecioBaseDia { get; set; }
    public string? ImagenReferencialUrl { get; set; }
    public string EstadoVehiculo { get; set; } = string.Empty;
    public int IdCategoriaVehiculo { get; set; }
    public int IdMarcaVehiculo { get; set; }

    public static VehiculoType FromDto(VehiculoDto dto) => new()
    {
        IdVehiculo = dto.id_vehiculo,
        CodigoInternoVehiculo = dto.codigo_interno_vehiculo,
        ModeloVehiculo = dto.modelo_vehiculo,
        AniofabricacIon = dto.anio_fabricacion,
        ColorVehiculo = dto.color_vehiculo,
        TipoCombustible = dto.tipo_combustible,
        TipoTransmision = dto.tipo_transmision,
        CapacidadPasajeros = dto.capacidad_pasajeros,
        CapacidadMaletas = dto.capacidad_maletas,
        NumeroPuertas = dto.numero_puertas,
        AireAcondicionado = dto.aire_acondicionado,
        PrecioBaseDia = dto.precio_base_dia,
        ImagenReferencialUrl = dto.imagen_referencial_url,
        EstadoVehiculo = dto.estado_vehiculo,
        IdCategoriaVehiculo = dto.id_categoria_vehiculo,
        IdMarcaVehiculo = dto.id_marca_vehiculo
    };
}