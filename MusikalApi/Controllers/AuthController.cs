using Microsoft.AspNetCore.Mvc;
using MusikalApi.DTOs;

namespace MusikalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private static readonly List<UsuarioSimuladoInfoDto> UsuariosMock = new()
    {
        new UsuarioSimuladoInfoDto
        {
            Usuario = "admin",
            Rol = "Admin",
            DescripcionPermisos = "Acceso total a catálogo (CRUD), configuración de pasarela, emisión de facturas y conciliación."
        },
        new UsuarioSimuladoInfoDto
        {
            Usuario = "vendedor",
            Rol = "Vendedor",
            DescripcionPermisos = "Consulta de catálogo y emisión de facturas."
        },
        new UsuarioSimuladoInfoDto
        {
            Usuario = "invitado",
            Rol = "Invitado",
            DescripcionPermisos = "Solo lectura de catálogo de instrumentos."
        }
    };

    /// <summary>
    /// [AUTENTICACIÓN SIMULADA] Inicia sesión con credenciales de prueba para obtener un token de acceso.
    /// Usuarios disponibles: 'admin' (pass: admin123), 'vendedor' (pass: vendedor123).
    /// </summary>
    [HttpPost("login")]
    public ActionResult<LoginResponseDto> Login([FromBody] LoginRequestDto request)
    {
        if (string.Equals(request.Usuario, "admin", StringComparison.OrdinalIgnoreCase) && request.Password == "admin123")
        {
            return Ok(new LoginResponseDto
            {
                Autenticado = true,
                Usuario = "admin",
                Rol = "Admin",
                Token = $"sim-token-admin-{Guid.NewGuid():N}",
                Mensaje = "Autenticación simulada exitosa con perfil de Administrador.",
                FechaExpiracion = DateTime.UtcNow.AddHours(8)
            });
        }

        if (string.Equals(request.Usuario, "vendedor", StringComparison.OrdinalIgnoreCase) && request.Password == "vendedor123")
        {
            return Ok(new LoginResponseDto
            {
                Autenticado = true,
                Usuario = "vendedor",
                Rol = "Vendedor",
                Token = $"sim-token-vendedor-{Guid.NewGuid():N}",
                Mensaje = "Autenticación simulada exitosa con perfil de Vendedor.",
                FechaExpiracion = DateTime.UtcNow.AddHours(8)
            });
        }

        return Unauthorized(new { mensaje = "Credenciales incorrectas. Pruebe con 'admin'/'admin123' o 'vendedor'/'vendedor123'." });
    }

    /// <summary>
    /// [AUTENTICACIÓN SIMULADA] Lista los usuarios simulados de prueba y sus roles asignados.
    /// </summary>
    [HttpGet("usuarios")]
    public ActionResult<IEnumerable<UsuarioSimuladoInfoDto>> GetUsuariosDisponibles()
    {
        return Ok(UsuariosMock);
    }

    /// <summary>
    /// [AUTENTICACIÓN SIMULADA] Consulta la identidad y rol del usuario según los encabezados enviados.
    /// </summary>
    [HttpGet("me")]
    public ActionResult GetPerfilActual()
    {
        string rol = "Invitado";
        string usuario = "anonimo";

        if (Request.Headers.TryGetValue("X-User-Role", out var roleHeader))
        {
            rol = roleHeader.ToString();
            usuario = rol.ToLower();
        }
        else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString().ToLower();
            if (token.Contains("admin"))
            {
                rol = "Admin";
                usuario = "admin";
            }
            else if (token.Contains("vendedor"))
            {
                rol = "Vendedor";
                usuario = "vendedor";
            }
        }

        return Ok(new
        {
            usuario,
            rol,
            autenticado = rol != "Invitado",
            descripcion = $"Sesión identificada con rol '{rol}'."
        });
    }
}
