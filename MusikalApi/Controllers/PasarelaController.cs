using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusikalApi.Data;
using MusikalApi.DTOs;
using MusikalApi.Models;

namespace MusikalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    /// Actualiza la configuración de la pasarela de pagos (URL base, credenciales/API Key, etc.).
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ConfiguracionPasarelaDto>> UpdateConfiguracion([FromBody] ConfiguracionPasarelaDto dto)
    {
        var config = await _context.ConfiguracionesPasarela.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new ConfiguracionPasarela();
            _context.ConfiguracionesPasarela.Add(config);
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
