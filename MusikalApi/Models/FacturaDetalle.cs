using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MusikalApi.Models;

public class FacturaDetalle
{
    public int Id { get; set; }

    public int FacturaId { get; set; }
    [JsonIgnore]
    public Factura? Factura { get; set; }

    public int ModeloInstrumentoId { get; set; }
    public ModeloInstrumento? ModeloInstrumento { get; set; }

    [Range(1, 10000, ErrorMessage = "La cantidad debe ser de al menos 1 unidad.")]
    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal => Cantidad * PrecioUnitario;
}
