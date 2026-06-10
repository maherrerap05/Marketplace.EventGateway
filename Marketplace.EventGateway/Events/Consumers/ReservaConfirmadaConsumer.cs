// Marketplace.EventGateway/Events/Consumers/ReservaConfirmadaConsumer.cs

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Marketplace.EventGateway.Events.Messages;
using Marketplace.EventGateway.Services;

namespace Marketplace.EventGateway.Events.Consumers;

/// <summary>
/// BackgroundService que consume eventos desde la cola q.reservas.confirmadas.
/// 
/// Responsabilidad: cuando MS.Reservas publica el resultado de la transacción
/// atómica (confirmada o rechazada), este consumer actualiza el estado en
/// memoria del EstadoReservaService para que el frontend pueda consultarlo
/// mediante polling via la query GraphQL consultarEstadoReserva.
/// 
/// Bindings específicos: solo escucha marketplace.reserva.confirmada y
/// marketplace.reserva.rechazada publicados por MS.Reservas.
/// No usa patrón wildcard para evitar capturar eventos de sincronización
/// de otros microservicios (MS.Catálogo, MS.Clientes).
/// </summary>
public class ReservaConfirmadaConsumer : BackgroundService
{
    private readonly IConnection? _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservaConfirmadaConsumer> _logger;

    private IChannel? _channel;

    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKeyConfirmada;
    private readonly string _routingKeyRechazada;
    private readonly string _dlxName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ReservaConfirmadaConsumer(
        IConnection? connection,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ReservaConfirmadaConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _exchangeName = configuration["RabbitMq:ExchangeName"]
            ?? throw new InvalidOperationException("RabbitMq:ExchangeName no configurado.");
        _queueName = configuration["RabbitMq:Queues:ReservasConfirmadas"]
            ?? throw new InvalidOperationException(
                "RabbitMq:Queues:ReservasConfirmadas no configurado.");
        _dlxName = configuration["RabbitMq:DeadLetterExchange"]
            ?? throw new InvalidOperationException(
                "RabbitMq:DeadLetterExchange no configurado.");
        _routingKeyConfirmada = configuration["RabbitMq:RoutingKeys:ReservaConfirmada"]
            ?? "marketplace.reserva.confirmada";
        _routingKeyRechazada = configuration["RabbitMq:RoutingKeys:ReservaRechazada"]
            ?? "marketplace.reserva.rechazada";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection is null)
        {
            _logger.LogWarning(
                "ReservaConfirmadaConsumer: Conexión RabbitMQ no disponible. " +
                "El consumidor está INACTIVO.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InicializarCanalYConsumirAsync(stoppingToken);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex,
                    "ReservaConfirmadaConsumer: Error en el canal. " +
                    "Reintentando en 30 segundos...");

                if (_channel is not null)
                {
                    try { await _channel.CloseAsync(stoppingToken); } catch { /* ignorar */ }
                    await _channel.DisposeAsync();
                    _channel = null;
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task InicializarCanalYConsumirAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection!.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        // Declarar el exchange principal (idempotente).
        await _channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Declarar el Dead Letter Exchange (idempotente).
        await _channel.ExchangeDeclareAsync(
            exchange: _dlxName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", _dlxName },
            { "x-message-ttl", 300000 }
        };

        // Declarar la cola q.reservas.confirmadas.
        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: stoppingToken);

        // ── Bindings específicos ──────────────────────────────────────────
        // NO usar wildcard marketplace.reserva.* porque capturaría eventos
        // de sincronización de MS.Catálogo y MS.Clientes que también
        // publican con routing keys marketplace.reserva.* hacia el exchange.
        // Solo escuchamos los dos eventos publicados por MS.Reservas.

        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: _routingKeyConfirmada,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: _routingKeyRechazada,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "ReservaConfirmadaConsumer: Canal listo. Escuchando cola {Queue} " +
            "con routing keys: {Confirmada} | {Rechazada}",
            _queueName, _routingKeyConfirmada, _routingKeyRechazada);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
            await ProcesarMensajeAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcesarMensajeAsync(
        BasicDeliverEventArgs ea,
        CancellationToken stoppingToken)
    {
        ReservaConfirmadaEvent? evento = null;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            evento = JsonSerializer.Deserialize<ReservaConfirmadaEvent>(body, JsonOptions);

            if (evento is null)
            {
                _logger.LogWarning(
                    "ReservaConfirmadaConsumer: Mensaje no deserializable. " +
                    "Se descarta con Nack.");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                return;
            }

            // Validar que el evento tiene un CorrelationId válido.
            // Eventos de otros microservicios tendrán CorrelationId vacío.
            if (evento.CorrelationId == Guid.Empty)
            {
                _logger.LogWarning(
                    "ReservaConfirmadaConsumer: Evento {EventId} descartado " +
                    "por CorrelationId vacío — no proviene de MS.Reservas.",
                    evento.EventId);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                return;
            }

            _logger.LogInformation(
                "ReservaConfirmadaConsumer: Procesando evento {EventId} | " +
                "CorrelationId: {CorrelationId} | Exitoso: {Exito}",
                evento.EventId, evento.CorrelationId, evento.Exito);

            using var scope = _scopeFactory.CreateScope();
            var estadoService = scope.ServiceProvider
                .GetRequiredService<IEstadoReservaService>();

            // Verificar que el CorrelationId existe en memoria.
            // Si no existe, el evento no corresponde a ninguna reserva
            // iniciada por este Gateway — se descarta.
            var estadoActual = estadoService.Obtener(evento.CorrelationId);
            if (estadoActual is null)
            {
                _logger.LogWarning(
                    "ReservaConfirmadaConsumer: CorrelationId {CorrelationId} " +
                    "no encontrado en memoria. Evento descartado.",
                    evento.CorrelationId);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                return;
            }

            // Si el estado ya es Confirmada, no sobreescribir con Rechazada.
            // Esto protege contra race conditions donde llegan mensajes
            // duplicados o fuera de orden.
            if (estadoActual.Estado == EstadoReserva.Confirmada && !evento.Exito)
            {
                _logger.LogWarning(
                    "ReservaConfirmadaConsumer: CorrelationId {CorrelationId} " +
                    "ya está Confirmada. Evento Rechazada descartado.",
                    evento.CorrelationId);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                return;
            }

            var nuevoEstado = new EstadoReservaDto
            {
                CorrelationId = evento.CorrelationId,
                Estado = evento.Exito
                    ? EstadoReserva.Confirmada
                    : EstadoReserva.Rechazada,
                CodigoReserva = evento.CodigoReserva,
                Mensaje = evento.Mensaje,
                CodigoError = evento.CodigoError,
                NumeroFactura = evento.Exito ? evento.NumeroFactura : null,
                EstadoFactura = evento.Exito ? evento.EstadoFactura : null,
                FechaEmisionFactura = evento.Exito ? evento.FechaEmisionFactura : null,
                FechaReservaUtc = evento.Exito ? evento.FechaReservaUtc : null,
                CantidadDias = evento.CantidadDias,
                SubtotalVehiculo = evento.SubtotalVehiculo,
                SubtotalExtras = evento.SubtotalExtras,
                Subtotal = evento.Subtotal,
                Iva = evento.Iva,
                Total = evento.Total
            };

            estadoService.Actualizar(nuevoEstado);

            await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

            _logger.LogInformation(
                "ReservaConfirmadaConsumer: Estado actualizado. " +
                "CorrelationId: {CorrelationId} | Estado: {Estado}",
                evento.CorrelationId,
                evento.Exito ? "Confirmada" : "Rechazada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ReservaConfirmadaConsumer: Error procesando evento {EventId}. " +
                "El mensaje pasa a DLX.",
                evento?.EventId);

            try
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx,
                    "ReservaConfirmadaConsumer: Error al enviar Nack para " +
                    "evento {EventId}.", evento?.EventId);
            }
        }
    }
}