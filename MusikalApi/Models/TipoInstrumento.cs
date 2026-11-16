using System.ComponentModel.DataAnnotations;

namespace MusikalApi.Models;

public class TipoInstrumento
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del tipo de instrumento es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    // Relación de navegación
    public List<ModeloInstrumento> Modelos { get; set; } = new();
}
