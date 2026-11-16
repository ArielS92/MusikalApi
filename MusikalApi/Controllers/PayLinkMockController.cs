using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusikalApi.Data;
using MusikalApi.DTOs;
using MusikalApi.Models;

namespace MusikalApi.Controllers;

[ApiController]
[Route("api/paylink-mock")]
[Produces("application/json")]
public class PayLinkMockController : ControllerBase
{
    private readonly MusikalDbContext _context;
    private readonly ILogger<PayLinkMockController> _logger;

    public PayLinkMockController(MusikalDbContext context, ILogger<PayLinkMockController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// [SIMULADOR PAYLINK] Endpoint para registrar una obligación de pago en la pasarela externa.
    /// Invocado por Musikal API al enviar una factura.
    /// </summary>
    [HttpPost("pagos")]
    public async Task<ActionResult<RegistroPagoPasarelaResponse>> RegistrarPagoEnPayLink([FromBody] RegistroPagoPasarelaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoPago))
        {
            return BadRequest(new { mensaje = "El Código de Pago es obligatorio." });
        }

        var existente = await _context.PayLinkPagosMock.FindAsync(request.CodigoPago);
        if (existente != null)
        {
            return Ok(new RegistroPagoPasarelaResponse
            {
                Exitoso = true,
                CodigoPago = request.CodigoPago,
                Estado = existente.Estado,
                Mensaje = "La obligación de pago ya se encontraba registrada previamente en PayLink."
            });
        }

        var pago = new PayLinkPagoMock
        {
            CodigoPago = request.CodigoPago,
            Monto = request.Monto,
            Descripcion = request.Descripcion,
            Estado = "PENDIENTE",
            FechaCreacion = DateTime.UtcNow
        };

        _context.PayLinkPagosMock.Add(pago);
        await _context.SaveChangesAsync();

        _logger.LogInformation("[PayLink Mock] Pago registrado: {Codigo}, Monto: {Monto}", request.CodigoPago, request.Monto);

        return Ok(new RegistroPagoPasarelaResponse
        {
            Exitoso = true,
            CodigoPago = request.CodigoPago,
            Estado = "PENDIENTE",
            Mensaje = "Obligación de pago registrada exitosamente en PayLink. Lista para pago del cliente."
        });
    }

    /// <summary>
    /// [SIMULADOR PAYLINK] Consulta el estado de un pago mediante su Código de Pago Único.
    /// Invocado por el proceso de conciliación.
    /// </summary>
    [HttpGet("pagos/{codigoPago}")]
    public async Task<ActionResult<ConsultaPagoPasarelaResponse>> ConsultarPago(string codigoPago)
    {
        var pago = await _context.PayLinkPagosMock.FindAsync(codigoPago);
        if (pago == null)
        {
            return NotFound(new { mensaje = $"No se encontró ningún pago con código '{codigoPago}' en PayLink." });
        }

        return Ok(new ConsultaPagoPasarelaResponse
        {
            CodigoPago = pago.CodigoPago,
            Monto = pago.Monto,
            Estado = pago.Estado,
            FechaPago = pago.FechaPago,
            Mensaje = $"Estado del pago: {pago.Estado}"
        });
    }

    /// <summary>
    /// [SIMULADOR PAYLINK] Endpoint de simulación para marcar un pago como PAGADO por el cliente.
    /// </summary>
    [HttpPost("pagos/{codigoPago}/pagar")]
    public async Task<ActionResult> SimularPagoCliente(string codigoPago, [FromQuery] string? metodoPago = "Tarjeta de Crédito")
    {
        var pago = await _context.PayLinkPagosMock.FindAsync(codigoPago);
        if (pago == null)
        {
            return NotFound(new { mensaje = $"El código de pago '{codigoPago}' no existe en PayLink." });
        }

        pago.Estado = "PAGADO";
        pago.FechaPago = DateTime.UtcNow;
        pago.MetodoPago = metodoPago ?? "Tarjeta de Crédito";

        await _context.SaveChangesAsync();

        _logger.LogInformation("[PayLink Mock] Pago simulado como PAGADO: {Codigo}", codigoPago);

        return Ok(new
        {
            mensaje = "El cliente completó exitosamente el pago en la pasarela PayLink.",
            codigoPago = pago.CodigoPago,
            estado = pago.Estado,
            fechaPago = pago.FechaPago,
            metodoPago = pago.MetodoPago
        });
    }

    /// <summary>
    /// [SIMULADOR PAYLINK] Endpoint de simulación para marcar un pago como RECHAZADO.
    /// </summary>
    [HttpPost("pagos/{codigoPago}/rechazar")]
    public async Task<ActionResult> SimularRechazo(string codigoPago)
    {
        var pago = await _context.PayLinkPagosMock.FindAsync(codigoPago);
        if (pago == null)
        {
            return NotFound(new { mensaje = $"El código de pago '{codigoPago}' no existe en PayLink." });
        }

        pago.Estado = "RECHAZADO";
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "El pago fue rechazado en la pasarela PayLink.",
            codigoPago = pago.CodigoPago,
            estado = pago.Estado
        });
    }

    /// <summary>
    /// [SIMULADOR PAYLINK] Lista todas las obligaciones de pago registradas en PayLink.
    /// </summary>
    [HttpGet("pagos")]
    public async Task<ActionResult<IEnumerable<PayLinkPagoMock>>> ListarPagos()
    {
        var pagos = await _context.PayLinkPagosMock.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        return Ok(pagos);
    }
}
