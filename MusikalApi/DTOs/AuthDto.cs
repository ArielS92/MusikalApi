using System.ComponentModel.DataAnnotations;

namespace MusikalApi.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Autenticado { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}

public class UsuarioSimuladoInfoDto
{
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string DescripcionPermisos { get; set; } = string.Empty;
}
