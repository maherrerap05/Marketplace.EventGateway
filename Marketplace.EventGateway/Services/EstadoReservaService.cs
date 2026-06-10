// Marketplace.EventGateway/Services/EstadoReservaService.cs

using System.Collections.Concurrent;

namespace Marketplace.EventGateway.Services;

/// <summary>
/// Estado posible de una reserva en el flujo asíncrono del Marketplace.
/// </summary>
public enum EstadoReserva
{
    EnProceso,
    Confirmada,
    Rechazada
}

/// <summary>
/// DTO que representa el estado actual de una reserva en memoria.
/// </summary>
public class EstadoReservaDto
{
    public Guid CorrelationId { get; set; }
    public EstadoReserva Estado { get; set; }
    public string CodigoReserva { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string CodigoError { get; set; } = string.Empty;

    // Datos disponibles una vez confirmada la reserva.
    public string? NumeroFactura { get; set; }
    public string? EstadoFactura { get; set; }
    public string? FechaEmisionFactura { get; set; }
    public string? FechaReservaUtc { get; set; }
    public int CantidadDias { get; set; }
    public double SubtotalVehiculo { get; set; }
    public double SubtotalExtras { get; set; }
    public double Subtotal { get; set; }
    public double Iva { get; set; }
    public double Total { get; set; }

    public DateTime FechaRegistroUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacionUtc { get; set; }
}

/// <summary>
/// Interfaz del servicio de estado de reservas en memoria.
/// </summary>
public interface IEstadoReservaService
{
    /// <summary>Registra una reserva en estado EnProceso.</summary>
    void RegistrarEnProceso(Guid correlationId, string codigoReserva);

    /// <summary>Actualiza el estado de una reserva existente.</summary>
    void Actualizar(EstadoReservaDto estadoDto);

    /// <summary>Obtiene el estado de una reserva por su correlationId.</summary>
    EstadoReservaDto? Obtener(Guid correlationId);
}

/// <summary>
/// Implementación en memoria del servicio de estado de reservas.
/// Usa ConcurrentDictionary para garantizar thread safety entre
/// el consumidor de RabbitMQ y las consultas HTTP del frontend.
///
/// Nota: el estado se pierde si el Gateway se reinicia. Esto es
/// aceptable para esta fase académica donde se usa estado en memoria.
/// </summary>
public class EstadoReservaService : IEstadoReservaService
{
    private readonly ConcurrentDictionary<Guid, EstadoReservaDto> _estados = new();
    private readonly ILogger<EstadoReservaService> _logger;

    public EstadoReservaService(ILogger<EstadoReservaService> logger)
    {
        _logger = logger;
    }

    public void RegistrarEnProceso(Guid correlationId, string codigoReserva)
    {
        var estado = new EstadoReservaDto
        {
            CorrelationId = correlationId,
            Estado = EstadoReserva.EnProceso,
            CodigoReserva = codigoReserva,
            Mensaje = "La reserva está siendo procesada.",
            FechaRegistroUtc = DateTime.UtcNow
        };

        _estados[correlationId] = estado;

        _logger.LogInformation(
            "EstadoReservaService: Reserva {CodigoReserva} registrada como EnProceso. " +
            "CorrelationId: {CorrelationId}",
            codigoReserva, correlationId);
    }

    public void Actualizar(EstadoReservaDto estadoDto)
    {
        estadoDto.FechaActualizacionUtc = DateTime.UtcNow;
        _estados[estadoDto.CorrelationId] = estadoDto;

        _logger.LogInformation(
            "EstadoReservaService: Reserva {CodigoReserva} actualizada a {Estado}. " +
            "CorrelationId: {CorrelationId}",
            estadoDto.CodigoReserva, estadoDto.Estado, estadoDto.CorrelationId);
    }

    public EstadoReservaDto? Obtener(Guid correlationId)
    {
        _estados.TryGetValue(correlationId, out var estado);
        return estado;
    }
}