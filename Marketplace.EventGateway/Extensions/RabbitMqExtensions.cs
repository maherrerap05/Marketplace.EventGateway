// Marketplace.EventGateway/Extensions/RabbitMqExtensions.cs

using RabbitMQ.Client;
using Marketplace.EventGateway.Events.Consumers;
using Marketplace.EventGateway.Events.Publishers;

namespace Marketplace.EventGateway.Extensions;

/// <summary>
/// Registra todos los servicios relacionados con RabbitMQ en el DI container.
/// Opción B: arranque tolerante. Si el broker no está disponible al iniciar,
/// el Gateway levanta normalmente y las queries GraphQL síncronas siguen
/// operativas. Solo las operaciones de escritura (mutation crearReserva)
/// quedarán inactivas hasta que el broker esté disponible.
/// </summary>
public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Registrar IConnection como Singleton ──────────────────────────
        services.AddSingleton<IConnection?>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ConnectionFactory>>();

            var host = configuration["RabbitMq:Host"];
            var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
            var username = configuration["RabbitMq:Username"] ?? "guest";
            var password = configuration["RabbitMq:Password"] ?? "guest";
            var port = int.Parse(configuration["RabbitMq:Port"] ?? "5672");

            if (string.IsNullOrWhiteSpace(host))
            {
                logger.LogWarning(
                    "RabbitMQ: RabbitMq:Host no configurado. " +
                    "El bus de eventos está DESHABILITADO en este entorno.");
                return null;
            }

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = host,
                    Port = port,
                    VirtualHost = virtualHost,
                    UserName = username,
                    Password = password,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                var connection = factory.CreateConnectionAsync()
                    .GetAwaiter().GetResult();

                logger.LogInformation(
                    "RabbitMQ: Conexión establecida con {Host}:{Port}/{VHost}",
                    host, port, virtualHost);

                return connection;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "RabbitMQ: No se pudo conectar con {Host}:{Port}/{VHost}. " +
                    "El bus de eventos está DESHABILITADO. " +
                    "Las queries GraphQL síncronas continúan operativas.",
                    host, port, virtualHost);
                return null;
            }
        });

        // ── Registrar el publicador como Singleton ────────────────────────
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // ── Registrar el consumidor como BackgroundService ────────────────
        services.AddHostedService<ReservaConfirmadaConsumer>();

        return services;
    }
}