using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusikalApi.Models;

public class ModeloInstrumento
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del modelo es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public int MarcaId { get; set; }
    public Marca? Marca { get; set; }

    [Required]
    public int TipoInstrumentoId { get; set; }
    public TipoInstrumento? TipoInstrumento { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "El precio debe ser mayor a 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Precio { get; set; }

    [Range(0, 100000, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }
}
