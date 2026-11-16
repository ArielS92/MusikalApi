using System.ComponentModel.DataAnnotations;

namespace MusikalApi.DTOs;

public class CrearTipoInstrumentoDto
{
    [Required(ErrorMessage = "El nombre del tipo de instrumento es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;
}

public class ActualizarTipoInstrumentoDto : CrearTipoInstrumentoDto
{
}

public class TipoInstrumentoResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadModelos { get; set; }
}
