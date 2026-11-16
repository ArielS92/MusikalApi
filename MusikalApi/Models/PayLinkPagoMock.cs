using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusikalApi.Models;

public class PayLinkPagoMock
{
    [Key]
    [StringLength(100)]
    public string CodigoPago { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [StringLength(255)]
    public string Descripcion { get; set; } = string.Empty;

    [StringLength(50)]
    public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, PAGADO, RECHAZADO

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaPago { get; set; }

    [StringLength(100)]
    public string? MetodoPago { get; set; }
}
