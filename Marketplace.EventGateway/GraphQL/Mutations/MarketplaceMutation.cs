// Marketplace.EventGateway/GraphQL/Mutations/MarketplaceMutation.cs

using Marketplace.EventGateway.Events.Messages;
using Marketplace.EventGateway.Events.Publishers;
using Marketplace.EventGateway.GraphQL.Inputs;
using Marketplace.EventGateway.GraphQL.Types;
using Marketplace.EventGateway.HttpClients;
using Marketplace.EventGateway.Services;

namespace Marketplace.EventGateway.GraphQL.Mutations;

/// <summary>
/// Mutations del Marketplace expuestas via GraphQL.
/// La mutation crearReserva es el núcleo del Gateway:
/// resuelve IDs, calcula totales, publica el evento y devuelve
/// inmediatamente un correlationId para polling.
/// </summary>
public class MarketplaceMutation
{
    /// <summary>
    /// Crea una reserva de forma asíncrona mediante el bus de eventos.
    ///
    /// Flujo:
    /// 1. Obtener token interno del ESB
    /// 2. Upsert cliente → MS.Clientes (obtiene id_cliente)
    /// 3. Upsert conductores → MS.Clientes (obtiene id_conductor[])
    /// 4. Consultar extras y vehículo en paralelo → MS.Catálogo
    /// 5. Calcular subtotales e IVA (15%)
    /// 6. Generar correlationId y codigoReserva
    /// 7. Publicar ReservaSolicitadaEvent → RabbitMQ
    /// 8. Registrar estado EN_PROCESO en memoria
    /// 9. Devolver { correlationId, codigoReserva, estado: EN_PROCESO }
    /// </summary>
    public async Task<ReservaResultadoType> CrearReserva(
        CrearReservaInput input,
        [Service] ClientesHttpClient clientesClient,
        [Service] CatalogoHttpClient catalogoClient,
        [Service] EsbHttpClient esbClient,
        [Service] IEventPublisher eventPublisher,
        [Service] IEstadoReservaService estadoService,
        [Service] IConfiguration configuration,
        [Service] ILogger<MarketplaceMutation> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "MarketplaceMutation: Iniciando creación de reserva para vehículo {IdVehiculo}",
            input.IdVehiculo);

        try
        {
            // ── Paso 1: Obtener token interno del ESB ─────────────────────
            var tokenJwt = await esbClient.ObtenerTokenInternoAsync(cancellationToken);

            // ── Paso 2: Upsert cliente ────────────────────────────────────
            var clienteRequest = new CrearClienteRequest
            {
                TipoIdentificacion = input.Cliente.TipoIdentificacion,
                NumeroIdentificacion = input.Cliente.NumeroIdentificacion,
                RazonSocial = input.Cliente.RazonSocial,
                Nombres = input.Cliente.Nombres,
                Apellidos = input.Cliente.Apellidos,
                Correo = input.Cliente.Correo,
                Telefono = input.Cliente.Telefono,
                Direccion = input.Cliente.Direccion
            };

            var idCliente = await clientesClient.UpsertClienteAsync(
                clienteRequest, tokenJwt, cancellationToken);

            logger.LogInformation(
                "MarketplaceMutation: Cliente resuelto. IdCliente: {IdCliente}",
                idCliente);

            // ── Paso 3: Upsert conductores ────────────────────────────────
            var conductoresResueltos = new List<(int IdConductor, bool EsPrincipal)>();

            foreach (var conductor in input.Conductores)
            {
                var conductorRequest = new CrearConductorRequest
                {
                    TipoIdentificacion = conductor.TipoIdentificacion,
                    NumeroIdentificacion = conductor.NumeroIdentificacion,
                    ConNombre1 = conductor.ConNombre1,
                    ConNombre2 = conductor.ConNombre2,
                    ConApellido1 = conductor.ConApellido1,
                    ConApellido2 = conductor.ConApellido2,
                    NumeroLicencia = conductor.NumeroLicencia,
                    FechaVencimientoLicencia = conductor.FechaVencimientoLicencia,
                    EdadConductor = conductor.EdadConductor,
                    ConTelefono = conductor.ConTelefono,
                    ConCorreo = conductor.ConCorreo
                };

                var idConductor = await clientesClient.UpsertConductorAsync(
                    conductorRequest, cancellationToken);

                conductoresResueltos.Add((idConductor, conductor.EsPrincipal));

                logger.LogInformation(
                    "MarketplaceMutation: Conductor resuelto. IdConductor: {IdConductor}",
                    idConductor);
            }

            // ── Paso 4: Consultar extras y vehículo en paralelo ───────────
            // Se ejecutan simultáneamente para reducir el tiempo total
            // de llamadas a MS.Catálogo.
            var extrasTask = catalogoClient.ObtenerExtrasAsync(cancellationToken);
            var vehiculoTask = catalogoClient.ObtenerVehiculoPorIdAsync(
                input.IdVehiculo, cancellationToken);

            await Task.WhenAll(extrasTask, vehiculoTask);

            var todosLosExtras = await extrasTask;
            var vehiculo = await vehiculoTask;

            if (vehiculo is null)
                throw new InvalidOperationException(
                    $"Vehículo {input.IdVehiculo} no encontrado en catálogo.");

            // ── Paso 5: Calcular totales ──────────────────────────────────
            var extrasEvento = new List<Events.Messages.ExtraEventItem>();
            double subtotalExtras = 0;

            foreach (var extra in input.Extras)
            {
                var extraDto = todosLosExtras
                    .FirstOrDefault(e => e.id_extra == extra.IdExtra);

                if (extraDto is null)
                {
                    logger.LogWarning(
                        "MarketplaceMutation: Extra {IdExtra} no encontrado en catálogo.",
                        extra.IdExtra);
                    continue;
                }

                var valorUnitario = (double)extraDto.valor_fijo;
                var subtotalExtra = valorUnitario * extra.Cantidad;
                subtotalExtras += subtotalExtra;

                extrasEvento.Add(new Events.Messages.ExtraEventItem
                {
                    IdExtra = extra.IdExtra,
                    Cantidad = extra.Cantidad,
                    ValorUnitario = valorUnitario,
                    Subtotal = subtotalExtra
                });
            }

            var cantidadDias = CalcularDias(input.FechaRecogida, input.FechaDevolucion);
            var subtotalVehiculo = (double)vehiculo.precio_base_dia * cantidadDias;
            var subtotalReserva = subtotalVehiculo + subtotalExtras;
            var valorIva = subtotalReserva * 0.15;
            var totalReserva = subtotalReserva + valorIva;

            logger.LogInformation(
                "MarketplaceMutation: Totales calculados. " +
                "Dias: {Dias} | SubtotalVehiculo: {SubV} | SubtotalExtras: {SubE} | " +
                "Subtotal: {Sub} | IVA: {Iva} | Total: {Total}",
                cantidadDias, subtotalVehiculo, subtotalExtras,
                subtotalReserva, valorIva, totalReserva);

            // ── Paso 6: Generar identificadores ───────────────────────────
            var correlationId = Guid.NewGuid();
            var codigoReserva = GenerarCodigoReserva();

            // ── Paso 7: Construir y publicar el evento ────────────────────
            var conductoresEvento = conductoresResueltos.Select(c =>
                new Events.Messages.ConductorEventItem
                {
                    IdConductor = c.IdConductor,
                    TipoConductor = c.EsPrincipal ? "PRI" : "ADI",
                    EsPrincipal = c.EsPrincipal,
                    EstadoReservaConductor = "ACT"
                }).ToList();

            var evento = new ReservaSolicitadaEvent
            {
                CorrelationId = correlationId,
                CodigoReserva = codigoReserva,
                IdCliente = idCliente,
                IdVehiculo = input.IdVehiculo,
                IdLocalizacionRecogida = input.IdLocalizacionRecogida,
                IdLocalizacionDevolucion = input.IdLocalizacionDevolucion,
                FechaRecogida = input.FechaRecogida,
                HoraRecogida = input.HoraRecogida,
                FechaDevolucion = input.FechaDevolucion,
                HoraDevolucion = input.HoraDevolucion,
                SubtotalVehiculo = subtotalVehiculo,
                SubtotalExtras = subtotalExtras,
                SubtotalReserva = subtotalReserva,
                ValorIva = valorIva,
                TotalReserva = totalReserva,
                Observaciones = input.Observaciones,
                Conductores = conductoresEvento,
                Extras = extrasEvento
            };

            var routingKey = configuration["RabbitMq:RoutingKeys:ReservaSolicitada"]
                ?? "marketplace.reserva.solicitada";

            await eventPublisher.PublicarAsync(evento, routingKey, cancellationToken);

            logger.LogInformation(
                "MarketplaceMutation: Evento publicado. " +
                "CorrelationId: {CorrelationId} | CodigoReserva: {CodigoReserva}",
                correlationId, codigoReserva);

            // ── Paso 8: Registrar estado EN_PROCESO en memoria ────────────
            estadoService.RegistrarEnProceso(correlationId, codigoReserva);

            // ── Paso 9: Devolver resultado inmediato ──────────────────────
            return new ReservaResultadoType
            {
                CorrelationId = correlationId,
                CodigoReserva = codigoReserva,
                Estado = "EN_PROCESO",
                Mensaje = "La reserva está siendo procesada. " +
                          "Consulta el estado con el correlationId proporcionado."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "MarketplaceMutation: Error al crear reserva para vehículo {IdVehiculo}",
                input.IdVehiculo);
            throw;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static int CalcularDias(string fechaRecogida, string fechaDevolucion)
    {
        if (DateOnly.TryParse(fechaRecogida, out var inicio) &&
            DateOnly.TryParse(fechaDevolucion, out var fin))
        {
            var dias = fin.DayNumber - inicio.DayNumber;
            return Math.Max(1, dias);
        }
        return 1;
    }

    private static string GenerarCodigoReserva()
    {
        var anio = DateTime.UtcNow.Year;
        var sufijo = Guid.NewGuid().ToString()[..8].ToUpper();
        return $"RC-{anio}-{sufijo}";
    }
}