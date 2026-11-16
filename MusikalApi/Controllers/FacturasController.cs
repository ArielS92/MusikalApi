using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusikalApi.Attributes;
using MusikalApi.Data;
using MusikalApi.DTOs;
using MusikalApi.Models;
using MusikalApi.Services;

namespace MusikalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FacturasController : ControllerBase
{
    private readonly MusikalDbContext _context;
    private readonly IPasarelaPagosService _pasarelaService;
    private readonly ILogger<FacturasController> _logger;

    public FacturasController(
        MusikalDbContext context,
        IPasarelaPagosService pasarelaService,
        ILogger<FacturasController> logger)
    {
        _context = context;
        _pasarelaService = pasarelaService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el listado de facturas emitidas, con filtro opcional por estado de pago.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FacturaResponseDto>>> GetFacturas([FromQuery] EstadoPagoFactura? estado)
    {
        var query = _context.Facturas
            .Include(f => f.Detalles)
                .ThenInclude(d => d.ModeloInstrumento)
            .AsQueryable();

        if (estado.HasValue)
        {
            query = query.Where(f => f.EstadoPago == estado.Value);
        }

        var facturas = await query
            .OrderByDescending(f => f.FechaEmision)
            .Select(f => MapFacturaToDto(f))
            .ToListAsync();

        return Ok(facturas);
    }

    /// <summary>
    /// Obtiene el detalle de una factura específica por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FacturaResponseDto>> GetFactura(int id)
    {
        var factura = await _context.Facturas
            .Include(f => f.Detalles)
                .ThenInclude(d => d.ModeloInstrumento)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factura == null)
        {
            return NotFound(new { mensaje = $"No se encontró la factura con ID {id}." });
        }

        return Ok(MapFacturaToDto(factura));
    }

    /// <summary>
    /// Genera una nueva factura y descuenta automáticamente el stock disponible.
    /// [¡Desafío!] Si el stock es insuficiente, se cancela la operación informando qué modelo presenta falta de stock.
    /// </summary>
    [HttpPost]
    [SimulatedAuthorize("Admin", "Vendedor")]
    public async Task<ActionResult<FacturaResponseDto>> CrearFactura([FromBody] CrearFacturaRequestDto request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest(new { mensaje = "La factura debe contener al menos un producto." });
        }

        // Agrupar items por ModeloInstrumentoId por si se enviaron repetidos
        var itemsAgrupados = request.Items
            .GroupBy(i => i.ModeloInstrumentoId)
            .Select(g => new { ModeloId = g.Key, CantidadTotal = g.Sum(x => x.Cantidad) })
            .ToList();

        var modeloIds = itemsAgrupados.Select(i => i.ModeloId).ToList();

        // Iniciamos transacción para asegurar atomicidad
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var modelos = await _context.Modelos
                .Where(m => modeloIds.Contains(m.Id))
                .ToListAsync();

            // Validar existencia de todos los modelos
            var faltantesEnCatalogo = modeloIds.Except(modelos.Select(m => m.Id)).ToList();
            if (faltantesEnCatalogo.Any())
            {
                return BadRequest(new
                {
                    mensaje = $"Los siguientes IDs de modelos no existen en el catálogo: {string.Join(", ", faltantesEnCatalogo)}"
                });
            }

            // [¡DESAFÍO CUMPLIDO!] Verificación estricta de stock disponible para cada modelo
            var erroresStock = new List<DetalleStockFaltanteDto>();

            foreach (var item in itemsAgrupados)
            {
                var modelo = modelos.First(m => m.Id == item.ModeloId);
                if (modelo.Stock < item.CantidadTotal)
                {
                    erroresStock.Add(new DetalleStockFaltanteDto
                    {
                        ModeloInstrumentoId = modelo.Id,
                        ModeloNombre = modelo.Nombre,
                        StockDisponible = modelo.Stock,
                        CantidadSolicitada = item.CantidadTotal
                    });
                }
            }

            // Si hay al menos un modelo sin stock suficiente, abortar e informar al cliente
            if (erroresStock.Any())
            {
                var modelosNombres = string.Join(", ", erroresStock.Select(e => $"'{e.ModeloNombre}' (Disponible: {e.StockDisponible}, Solicitado: {e.CantidadSolicitada})"));
                return StatusCode(StatusCodes.Status409Conflict, new ErrorStockInsuficienteDto
                {
                    Mensaje = $"No es posible generar la factura debido a stock insuficiente en: {modelosNombres}.",
                    ErroresStock = erroresStock
                });
            }

            // Crear la factura
            var factura = new Factura
            {
                FechaEmision = DateTime.UtcNow,
                ClienteId = request.ClienteId.Trim(),
                ClienteNombre = request.ClienteNombre?.Trim() ?? $"Cliente #{request.ClienteId.Trim()}",
                CodigoPagoUnico = Guid.NewGuid().ToString("N"),
                EstadoPago = EstadoPagoFactura.Pendiente,
                RegistradaEnPasarela = false
            };

            decimal montoTotal = 0;

            // Descontar stock y armar detalles
            foreach (var item in itemsAgrupados)
            {
                var modelo = modelos.First(m => m.Id == item.ModeloId);

                // Descuento automático de stock
                modelo.Stock -= item.CantidadTotal;

                var detalle = new FacturaDetalle
                {
                    Factura = factura,
                    ModeloInstrumentoId = modelo.Id,
                    ModeloInstrumento = modelo,
                    Cantidad = item.CantidadTotal,
                    PrecioUnitario = modelo.Precio
                };

                montoTotal += detalle.Subtotal;
                factura.Detalles.Add(detalle);
            }

            factura.MontoTotal = montoTotal;

            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var responseDto = MapFacturaToDto(factura);
            return CreatedAtAction(nameof(GetFactura), new { id = factura.Id }, responseDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al generar la factura.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error interno al procesar la factura.", detalle = ex.Message });
        }
    }

    /// <summary>
    /// Envía la factura a la pasarela de pagos externa ("PayLink") para registrar la obligación de pago.
    /// </summary>
    /// <param name="id">ID de la factura a enviar.</param>
    [HttpPost("{id}/enviar-pasarela")]
    [HttpPost("{id}/enviar")]
    [SimulatedAuthorize("Admin", "Vendedor")]
    public async Task<ActionResult<RegistroPagoPasarelaResponse>> EnviarAFacturaPasarela(int id)
    {
        var factura = await _context.Facturas
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factura == null)
        {
            return NotFound(new { mensaje = $"No se encontró la factura con ID {id}." });
        }

        if (factura.EstadoPago == EstadoPagoFactura.Pagada)
        {
            return BadRequest(new { mensaje = $"La factura #{id} ya se encuentra pagada." });
        }

        var descripcion = $"Pago Factura #{factura.Id} - Cliente: {factura.ClienteNombre} ({factura.ClienteId})";

        var respuestaPasarela = await _pasarelaService.RegistrarPagoAsync(
            factura.CodigoPagoUnico,
            factura.MontoTotal,
            descripcion
        );

        if (!respuestaPasarela.Exitoso)
        {
            return StatusCode(StatusCodes.Status502BadGateway, respuestaPasarela);
        }

        factura.RegistradaEnPasarela = true;
        factura.FechaRegistroPasarela = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(respuestaPasarela);
    }

    /// <summary>
    /// Mecanismo de conciliación de pagos con la pasarela PayLink.
    /// Recupera todas las facturas en estado 'Pendiente', consulta el estado en la pasarela externa,
    /// y si la pasarela confirma el pago, actualiza el estado interno a 'Pagada'.
    /// Soporta tanto POST /api/facturas/reconciliar como POST /reconcile.
    /// </summary>
    [HttpPost("reconciliar")]
    [HttpPost("/reconcile")]
    [SimulatedAuthorize("Admin")]
    public async Task<ActionResult<ResultadoConciliacionDto>> ReconciliarPagos()
    {
        var facturasPendientes = await _context.Facturas
            .Where(f => f.EstadoPago == EstadoPagoFactura.Pendiente)
            .ToListAsync();

        var resultado = new ResultadoConciliacionDto
        {
            TotalFacturasPendientesRevisadas = facturasPendientes.Count
        };

        foreach (var factura in facturasPendientes)
        {
            var consulta = await _pasarelaService.ConsultarEstadoPagoAsync(factura.CodigoPagoUnico);

            var detalleItem = new DetalleConciliacionItemDto
            {
                FacturaId = factura.Id,
                CodigoPagoUnico = factura.CodigoPagoUnico,
                Monto = factura.MontoTotal,
                EstadoAnterior = factura.EstadoPago.ToString()
            };

            if (consulta == null)
            {
                detalleItem.EstadoEnPasarela = "SIN_RESPUESTA";
                detalleItem.EstadoNuevo = factura.EstadoPago.ToString();
                detalleItem.Observacion = "No hubo respuesta del servicio de la pasarela.";
                resultado.FacturasConErrorConsulta++;
            }
            else if (consulta.Estado.Equals("PAGADO", StringComparison.OrdinalIgnoreCase) ||
                     consulta.Estado.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
                     consulta.Estado.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                // Pasarela confirmó el pago
                factura.EstadoPago = EstadoPagoFactura.Pagada;
                factura.FechaPago = consulta.FechaPago ?? DateTime.UtcNow;

                detalleItem.EstadoEnPasarela = consulta.Estado;
                detalleItem.EstadoNuevo = EstadoPagoFactura.Pagada.ToString();
                detalleItem.Observacion = "Pago verificado y conciliado exitosamente.";
                resultado.FacturasConciliadasPagadas++;
            }
            else
            {
                detalleItem.EstadoEnPasarela = consulta.Estado;
                detalleItem.EstadoNuevo = factura.EstadoPago.ToString();
                detalleItem.Observacion = consulta.Mensaje ?? $"El pago continúa en estado '{consulta.Estado}'.";
                resultado.FacturasAunPendientes++;
            }

            resultado.Detalles.Add(detalleItem);
        }

        await _context.SaveChangesAsync();
        return Ok(resultado);
    }

    private static FacturaResponseDto MapFacturaToDto(Factura factura)
    {
        return new FacturaResponseDto
        {
            Id = factura.Id,
            FechaEmision = factura.FechaEmision,
            ClienteId = factura.ClienteId,
            ClienteNombre = factura.ClienteNombre,
            MontoTotal = factura.MontoTotal,
            CodigoPagoUnico = factura.CodigoPagoUnico,
            EstadoPago = factura.EstadoPago.ToString(),
            RegistradaEnPasarela = factura.RegistradaEnPasarela,
            FechaRegistroPasarela = factura.FechaRegistroPasarela,
            FechaPago = factura.FechaPago,
            Detalles = factura.Detalles.Select(d => new FacturaDetalleResponseDto
            {
                Id = d.Id,
                ModeloInstrumentoId = d.ModeloInstrumentoId,
                ModeloNombre = d.ModeloInstrumento != null ? d.ModeloInstrumento.Nombre : string.Empty,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Subtotal
            }).ToList()
        };
    }
}
