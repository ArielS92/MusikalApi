using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusikalApi.Data;
using MusikalApi.DTOs;
using MusikalApi.Models;

namespace MusikalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TiposController : ControllerBase
{
    private readonly MusikalDbContext _context;

    public TiposController(MusikalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene la lista completa de tipos de instrumento.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoInstrumentoResponseDto>>> GetTipos()
    {
        var tipos = await _context.TiposInstrumento
            .Select(t => new TipoInstrumentoResponseDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                CantidadModelos = t.Modelos.Count
            })
            .ToListAsync();

        return Ok(tipos);
    }

    /// <summary>
    /// Obtiene un tipo de instrumento específico por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TipoInstrumentoResponseDto>> GetTipo(int id)
    {
        var tipo = await _context.TiposInstrumento
            .Include(t => t.Modelos)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tipo == null)
        {
            return NotFound(new { mensaje = $"No se encontró el tipo de instrumento con ID {id}." });
        }

        return Ok(new TipoInstrumentoResponseDto
        {
            Id = tipo.Id,
            Nombre = tipo.Nombre,
            CantidadModelos = tipo.Modelos.Count
        });
    }

    /// <summary>
    /// Registra un nuevo tipo de instrumento.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TipoInstrumentoResponseDto>> CreateTipo([FromBody] CrearTipoInstrumentoDto dto)
    {
        if (await _context.TiposInstrumento.AnyAsync(t => t.Nombre.ToLower() == dto.Nombre.ToLower()))
        {
            return Conflict(new { mensaje = $"Ya existe un tipo de instrumento registrado con el nombre '{dto.Nombre}'." });
        }

        var tipo = new TipoInstrumento
        {
            Nombre = dto.Nombre.Trim()
        };

        _context.TiposInstrumento.Add(tipo);
        await _context.SaveChangesAsync();

        var response = new TipoInstrumentoResponseDto
        {
            Id = tipo.Id,
            Nombre = tipo.Nombre,
            CantidadModelos = 0
        };

        return CreatedAtAction(nameof(GetTipo), new { id = tipo.Id }, response);
    }

    /// <summary>
    /// Actualiza los datos de un tipo de instrumento.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TipoInstrumentoResponseDto>> UpdateTipo(int id, [FromBody] ActualizarTipoInstrumentoDto dto)
    {
        var tipo = await _context.TiposInstrumento.Include(t => t.Modelos).FirstOrDefaultAsync(t => t.Id == id);
        if (tipo == null)
        {
            return NotFound(new { mensaje = $"No se encontró el tipo de instrumento con ID {id}." });
        }

        if (await _context.TiposInstrumento.AnyAsync(t => t.Nombre.ToLower() == dto.Nombre.ToLower() && t.Id != id))
        {
            return Conflict(new { mensaje = $"Ya existe otro tipo de instrumento con el nombre '{dto.Nombre}'." });
        }

        tipo.Nombre = dto.Nombre.Trim();
        await _context.SaveChangesAsync();

        return Ok(new TipoInstrumentoResponseDto
        {
            Id = tipo.Id,
            Nombre = tipo.Nombre,
            CantidadModelos = tipo.Modelos.Count
        });
    }

    /// <summary>
    /// Elimina un tipo de instrumento si no tiene modelos asociados.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTipo(int id)
    {
        var tipo = await _context.TiposInstrumento
            .Include(t => t.Modelos)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tipo == null)
        {
            return NotFound(new { mensaje = $"No se encontró el tipo de instrumento con ID {id}." });
        }

        if (tipo.Modelos.Any())
        {
            return BadRequest(new { mensaje = $"No se puede eliminar el tipo '{tipo.Nombre}' porque tiene {tipo.Modelos.Count} modelo(s) asociado(s)." });
        }

        _context.TiposInstrumento.Remove(tipo);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Tipo de instrumento '{tipo.Nombre}' eliminado exitosamente." });
    }
}
