using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusikalApi.Attributes;
using MusikalApi.Data;
using MusikalApi.DTOs;
using MusikalApi.Models;

namespace MusikalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SimulatedAuthorize("Admin")]
public class PasarelaController : ControllerBase
{
    private readonly MusikalDbContext _context;

    public PasarelaController(MusikalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene la configuración actual de la pasarela de pagos.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ConfiguracionPasarelaDto>> GetConfiguracion()
    {
        var config = await _context.ConfiguracionesPasarela.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new ConfiguracionPasarela();
            _context.ConfiguracionesPasarela.Add(config);
            await _context.SaveChangesAsync();
        }

        return Ok(new ConfiguracionPasarelaDto
        {
            Id = config.Id,
            Nombre = config.Nombre,
            UrlBase = config.UrlBase,
            ApiKey = config.ApiKey,
            Activa = config.Activa
        });
    }

    /// <summary>
    /// Obtiene una configuración de pasarela específica por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ConfiguracionPasarelaDto>> GetConfiguracionPorId(int id)
    {
        var config = await _context.ConfiguracionesPasarela.FindAsync(id);
        if (config == null)
        {
            return NotFound(new { mensaje = $"No se encontró la configuración de pasarela con ID {id}." });
        }

        return Ok(new ConfiguracionPasarelaDto
        {
            Id = config.Id,
            Nombre = config.Nombre,
            UrlBase = config.UrlBase,
            ApiKey = config.ApiKey,
            Activa = config.Activa
        });
    }

    /// <summary>
    /// Registra una nueva configuración de pasarela de pagos.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConfiguracionPasarelaDto>> CreateConfiguracion([FromBody] ConfiguracionPasarelaDto dto)
    {
        var config = new ConfiguracionPasarela
        {
            Nombre = dto.Nombre.Trim(),
            UrlBase = dto.UrlBase.Trim(),
            ApiKey = dto.ApiKey.Trim(),
            Activa = dto.Activa
        };

        if (config.Activa)
        {
            // Si la nueva configuración está activa, desactivar las demás
            var activas = await _context.ConfiguracionesPasarela.Where(c => c.Activa).ToListAsync();
            foreach (var act in activas)
            {
                act.Activa = false;
            }
        }

        _context.ConfiguracionesPasarela.Add(config);
        await _context.SaveChangesAsync();

        dto.Id = config.Id;
        return CreatedAtAction(nameof(GetConfiguracionPorId), new { id = config.Id }, dto);
    }

    /// <summary>
    /// Actualiza la configuración de la pasarela de pagos (URL base, credenciales/API Key, etc.).
    /// </summary>
    [HttpPut]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConfiguracionPasarelaDto>> UpdateConfiguracion([FromBody] ConfiguracionPasarelaDto dto, [FromRoute] int? id = null)
    {
        ConfiguracionPasarela? config;
        if (id.HasValue)
        {
            config = await _context.ConfiguracionesPasarela.FindAsync(id.Value);
            if (config == null)
            {
                return NotFound(new { mensaje = $"No se encontró la configuración con ID {id.Value}." });
            }
        }
        else
        {
            config = await _context.ConfiguracionesPasarela.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfiguracionPasarela();
                _context.ConfiguracionesPasarela.Add(config);
            }
        }

        config.Nombre = dto.Nombre.Trim();
        config.UrlBase = dto.UrlBase.Trim();
        config.ApiKey = dto.ApiKey.Trim();
        config.Activa = dto.Activa;

        await _context.SaveChangesAsync();

        return Ok(new ConfiguracionPasarelaDto
        {
            Id = config.Id,
            Nombre = config.Nombre,
            UrlBase = config.UrlBase,
            ApiKey = config.ApiKey,
            Activa = config.Activa
        });
    }
}
