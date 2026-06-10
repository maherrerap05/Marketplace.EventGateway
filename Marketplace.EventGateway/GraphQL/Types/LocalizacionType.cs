// Marketplace.EventGateway/GraphQL/Types/LocalizacionType.cs

using Marketplace.EventGateway.HttpClients;

namespace Marketplace.EventGateway.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa una localización del Marketplace.
/// </summary>
public class LocalizacionType
{
    public int IdLocalizacion { get; set; }
    public string CodigoLocalizacion { get; set; } = string.Empty;
    public string NombreLocalizacion { get; set; } = string.Empty;
    public string DireccionLocalizacion { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public string CorreoContacto { get; set; } = string.Empty;
    public string HorarioAtencion { get; set; } = string.Empty;
    public string ZonaHoraria { get; set; } = string.Empty;
    public string EstadoLocalizacion { get; set; } = string.Empty;

    public static LocalizacionType FromDto(LocalizacionDto dto) => new()
    {
        IdLocalizacion = dto.id_localizacion,
        CodigoLocalizacion = dto.codigo_localizacion,
        NombreLocalizacion = dto.nombre_localizacion,
        DireccionLocalizacion = dto.direccion_localizacion,
        TelefonoContacto = dto.telefono_contacto,
        CorreoContacto = dto.correo_contacto,
        HorarioAtencion = dto.horario_atencion,
        ZonaHoraria = dto.zona_horaria,
        EstadoLocalizacion = dto.estado_localizacion
    };
}