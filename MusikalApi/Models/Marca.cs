using System.ComponentModel.DataAnnotations;

namespace MusikalApi.Models;

public class Marca
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país de origen es obligatorio.")]
    [StringLength(100)]
    public string PaisOrigen { get; set; } = string.Empty;

    // Relación de navegación
    public List<ModeloInstrumento> Modelos { get; set; } = new();
}
