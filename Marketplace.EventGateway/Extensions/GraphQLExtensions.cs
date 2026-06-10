// Marketplace.EventGateway/Extensions/GraphQLExtensions.cs

using Marketplace.EventGateway.GraphQL.Mutations;
using Marketplace.EventGateway.GraphQL.Queries;
using Marketplace.EventGateway.Services;

namespace Marketplace.EventGateway.Extensions;

/// <summary>
/// Registra HotChocolate GraphQL en el DI container.
/// Configura el schema con las queries y mutations del Marketplace.
/// </summary>
public static class GraphQLExtensions
{
    public static IServiceCollection AddGraphQL(
        this IServiceCollection services)
    {
        // Registrar el servicio de estado como Singleton.
        // Singleton porque mantiene el estado en memoria entre requests.
        services.AddSingleton<IEstadoReservaService, EstadoReservaService>();

        // Configurar HotChocolate.
        services
            .AddGraphQLServer()
            .AddQueryType<MarketplaceQuery>()
            .AddMutationType<MarketplaceMutation>();

        return services;
    }
}