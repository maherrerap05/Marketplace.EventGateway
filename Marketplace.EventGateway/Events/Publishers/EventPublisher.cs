// Marketplace.EventGateway/Events/Publishers/RabbitMqEventPublisher.cs

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Marketplace.EventGateway.Events.Publishers;

/// <summary>
/// Implementación del publicador de eventos usando RabbitMQ.Client v7.
/// Publica mensajes persistentes hacia el exchange Topic configurado.
/// Si la conexión no está disponible, loguea el error sin lanzar excepción.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private IChannel? _channel;
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConnection? _connection;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqEventPublisher(
        IConnection? connection,
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _exchangeName = configuration["RabbitMq:ExchangeName"]
            ?? throw new InvalidOperationException("RabbitMq:ExchangeName no configurado.");

        _ = InicializarCanalAsync();
    }

    private async Task InicializarCanalAsync()
    {
        if (_connection is null) return;

        try
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation(
                "RabbitMQ Publisher: Canal inicializado. Exchange: {Exchange}",
                _exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RabbitMQ Publisher: No se pudo inicializar el canal. " +
                "Las publicaciones quedarán descartadas hasta reconexión.");
            _channel = null;
        }
    }

    public async Task PublicarAsync<T>(
        T evento,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            _logger.LogWarning(
                "RabbitMQ Publisher: Intento de publicar en publisher disposed. " +
                "RoutingKey: {RoutingKey}", routingKey);
            return;
        }

        if (_channel is null)
            await InicializarCanalAsync();

        if (_channel is null)
        {
            _logger.LogWarning(
                "RabbitMQ Publisher: Canal no disponible. Evento {Tipo} descartado. " +
                "RoutingKey: {RoutingKey}", typeof(T).Name, routingKey);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(evento, JsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "RabbitMQ Publisher: Evento publicado. RoutingKey: {RoutingKey} | Tipo: {Tipo}",
                routingKey, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RabbitMQ Publisher: Error al publicar evento {Tipo}. RoutingKey: {RoutingKey}",
                typeof(T).Name, routingKey);
            _channel = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_channel is not null)
        {
            try { await _channel.CloseAsync(); } catch { /* ignorar */ }
            try { await _channel.DisposeAsync(); } catch { /* ignorar */ }
        }
    }
}