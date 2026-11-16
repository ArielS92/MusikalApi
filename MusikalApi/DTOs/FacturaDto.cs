using System.ComponentModel.DataAnnotations;

namespace MusikalApi.DTOs;

public class CrearFacturaItemDto
{
    [Required(ErrorMessage = "El ID del modelo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del modelo no es válido.")]
    public int ModeloInstrumentoId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 10000, ErrorMessage = "La cantidad debe ser de al menos 1 unidad.")]
    public int Cantidad { get; set; }
}

public class CrearFacturaRequestDto
{
    [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string ClienteId { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ClienteNombre { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un producto en la factura.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un producto en la factura.")]
    public List<CrearFacturaItemDto> Items { get; set; } = new();
}

public class FacturaDetalleResponseDto
{
    public int Id { get; set; }
    public int ModeloInstrumentoId { get; set; }
    public string ModeloNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class FacturaResponseDto
{
    public int Id { get; set; }
    public DateTime FechaEmision { get; set; }
    public string ClienteId { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string CodigoPagoUnico { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public bool RegistradaEnPasarela { get; set; }
    public DateTime? FechaRegistroPasarela { get; set; }
    public DateTime? FechaPago { get; set; }
    public List<FacturaDetalleResponseDto> Detalles { get; set; } = new();
}

public class ErrorStockInsuficienteDto
{
    public string Mensaje { get; set; } = "Stock insuficiente para procesar la factura.";
    public List<DetalleStockFaltanteDto> ErroresStock { get; set; } = new();
}

public class DetalleStockFaltanteDto
{
    public int ModeloInstrumentoId { get; set; }
    public string ModeloNombre { get; set; } = string.Empty;
    public int StockDisponible { get; set; }
    public int CantidadSolicitada { get; set; }
    public int Faltante => CantidadSolicitada - StockDisponible;
}
