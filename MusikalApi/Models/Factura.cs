using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusikalApi.Models;

public enum EstadoPagoFactura
{
    Pendiente,
    Pagada,
    Cancelada
}

public class Factura
{
    public int Id { get; set; }

    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string ClienteId { get; set; } = string.Empty;

    [StringLength(150)]
    public string ClienteNombre { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoTotal { get; set; }

    /// <summary>
    /// Código de Pago Único utilizado para vincular e interactuar con la pasarela de pagos (PayLink).
    /// </summary>
    [Required]
    [StringLength(100)]
    public string CodigoPagoUnico { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public EstadoPagoFactura EstadoPago { get; set; } = EstadoPagoFactura.Pendiente;

    public bool RegistradaEnPasarela { get; set; } = false;

    public DateTime? FechaRegistroPasarela { get; set; }

    public DateTime? FechaPago { get; set; }

    public List<FacturaDetalle> Detalles { get; set; } = new();
}
