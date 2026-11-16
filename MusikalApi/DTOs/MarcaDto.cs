using System.ComponentModel.DataAnnotations;

namespace MusikalApi.DTOs;

public class CrearMarcaDto
{
    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país de origen es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string PaisOrigen { get; set; } = string.Empty;
}

public class ActualizarMarcaDto : CrearMarcaDto
{
}

public class MarcaResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PaisOrigen { get; set; } = string.Empty;
    public int CantidadModelos { get; set; }
}
