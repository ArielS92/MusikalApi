using System.ComponentModel.DataAnnotations;

namespace MusikalApi.Models;

public class ConfiguracionPasarela
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = "PayLink";

    [Required]
    [StringLength(255)]
    public string UrlBase { get; set; } = "http://localhost:5000/api/paylink-mock";

    [StringLength(255)]
    public string ApiKey { get; set; } = "paylink_live_secret_key_musikal_2026";

    public bool Activa { get; set; } = true;
}
