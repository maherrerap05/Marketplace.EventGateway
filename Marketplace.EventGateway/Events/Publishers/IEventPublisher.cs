// Marketplace.EventGateway/Events/Publishers/IEventPublisher.cs

namespace Marketplace.EventGateway.Events.Publishers;

/// <summary>
/// Contrato del publicador de eventos hacia RabbitMQ.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publica un evento serializado como JSON hacia el exchange configurado.
    /// </summary>
    /// <typeparam name="T">Tipo del evento a publicar.</typeparam>
    /// <param name="evento">Instancia del evento a serializar y publicar.</param>
    /// <param name="routingKey">
    /// Routing key para el exchange Topic.
    /// Ej: "marketplace.reserva.solicitada"
    /// </param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task PublicarAsync<T>(T evento, string routingKey, CancellationToken cancellationToken = default);
}