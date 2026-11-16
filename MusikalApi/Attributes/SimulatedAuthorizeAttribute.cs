using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MusikalApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class SimulatedAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _rolesRequeridos;

    public SimulatedAuthorizeAttribute(params string[] rolesRequeridos)
    {
        _rolesRequeridos = rolesRequeridos;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var modoEstricto = config.GetValue<bool>("Auth:ModoEstricto", false);

        var req = context.HttpContext.Request;

        // Extraer rol o token simulado
        string? rol = null;

        if (req.Headers.TryGetValue("X-User-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader))
        {
            rol = roleHeader.ToString().Trim();
        }
        else if (req.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
        {
            var token = authHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (token.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                rol = "Admin";
            }
            else if (token.Contains("vendedor", StringComparison.OrdinalIgnoreCase))
            {
                rol = "Vendedor";
            }
            else if (!string.IsNullOrEmpty(token))
            {
                rol = "Usuario";
            }
        }

        // Si el modo estricto está habilitado o el cliente proactivamente envió credenciales simuladas:
        if (modoEstricto)
        {
            if (string.IsNullOrEmpty(rol))
            {
                context.Result = new ObjectResult(new
                {
                    mensaje = "Acceso no autorizado. Se requiere autenticación simulada mediante header 'Authorization: Bearer <token>' o 'X-User-Role: Admin|Vendedor'. Use POST /api/auth/login para obtener credenciales."
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            if (_rolesRequeridos.Length > 0 && !_rolesRequeridos.Any(r => r.Equals(rol, StringComparison.OrdinalIgnoreCase)))
            {
                context.Result = new ObjectResult(new
                {
                    mensaje = $"Acceso denegado (403 Prohibido). Se requiere uno de los siguientes roles: {string.Join(", ", _rolesRequeridos)}. Su rol actual es '{rol}'."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }
        else if (!string.IsNullOrEmpty(rol) && _rolesRequeridos.Length > 0)
        {
            // Si no está en modo estricto pero el usuario explicitó un rol inferior (por ejemplo para testear 403 Forbidden):
            if (!_rolesRequeridos.Any(r => r.Equals(rol, StringComparison.OrdinalIgnoreCase)))
            {
                context.Result = new ObjectResult(new
                {
                    mensaje = $"Acceso denegado (403 Prohibido). Se requiere uno de los siguientes roles: {string.Join(", ", _rolesRequeridos)}. Su rol actual es '{rol}'."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        // Usuario autorizado, continuar pipeline
        await next();
    }
}
