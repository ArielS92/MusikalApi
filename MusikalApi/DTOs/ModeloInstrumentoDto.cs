using System.ComponentModel.DataAnnotations;

namespace MusikalApi.DTOs;

public class CrearModeloInstrumentoDto
{
    [Required(ErrorMessage = "El nombre del modelo es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El ID de la marca es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de marca no es válido.")]
    public int MarcaId { get; set; }

    [Required(ErrorMessage = "El ID del tipo de instrumento es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de tipo de instrumento no es válido.")]
    public int TipoInstrumentoId { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 100000000, ErrorMessage = "El precio de venta debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, 100000, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }
}

public class ActualizarModeloInstrumentoDto : CrearModeloInstrumentoDto
{
}

public class ModeloInstrumentoResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int MarcaId { get; set; }
    public string MarcaNombre { get; set; } = string.Empty;
    public int TipoInstrumentoId { get; set; }
    public string TipoInstrumentoNombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}
