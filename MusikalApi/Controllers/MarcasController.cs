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
public class MarcasController : ControllerBase
{
    private readonly MusikalDbContext _context;

    public MarcasController(MusikalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene la lista completa de marcas de instrumentos.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarcaResponseDto>>> GetMarcas()
    {
        var marcas = await _context.Marcas
            .Select(m => new MarcaResponseDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                PaisOrigen = m.PaisOrigen,
                CantidadModelos = m.Modelos.Count
            })
            .ToListAsync();

        return Ok(marcas);
    }

    /// <summary>
    /// Obtiene los detalles de una marca específica por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MarcaResponseDto>> GetMarca(int id)
    {
        var marca = await _context.Marcas
            .Include(m => m.Modelos)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (marca == null)
        {
            return NotFound(new { mensaje = $"No se encontró la marca con ID {id}." });
        }

        return Ok(new MarcaResponseDto
        {
            Id = marca.Id,
            Nombre = marca.Nombre,
            PaisOrigen = marca.PaisOrigen,
            CantidadModelos = marca.Modelos.Count
        });
    }

    /// <summary>
    /// Obtiene todos los modelos asociados a una marca específica.
    /// </summary>
    [HttpGet("{id}/modelos")]
    public async Task<ActionResult<IEnumerable<ModeloInstrumentoResponseDto>>> GetModelosPorMarca(int id)
    {
        var marcaExiste = await _context.Marcas.AnyAsync(m => m.Id == id);
        if (!marcaExiste)
        {
            return NotFound(new { mensaje = $"No se encontró la marca con ID {id}." });
        }

        var modelos = await _context.Modelos
            .Where(m => m.MarcaId == id)
            .Include(m => m.Marca)
            .Include(m => m.TipoInstrumento)
            .Select(m => new ModeloInstrumentoResponseDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                MarcaId = m.MarcaId,
                MarcaNombre = m.Marca != null ? m.Marca.Nombre : string.Empty,
                TipoInstrumentoId = m.TipoInstrumentoId,
                TipoInstrumentoNombre = m.TipoInstrumento != null ? m.TipoInstrumento.Nombre : string.Empty,
                Precio = m.Precio,
                Stock = m.Stock
            })
            .ToListAsync();

        return Ok(modelos);
    }

    /// <summary>
    /// Registra una nueva marca.
    /// </summary>
    [HttpPost]
    [SimulatedAuthorize("Admin")]
    public async Task<ActionResult<MarcaResponseDto>> CreateMarca([FromBody] CrearMarcaDto dto)
    {
        if (await _context.Marcas.AnyAsync(m => m.Nombre.ToLower() == dto.Nombre.ToLower()))
        {
            return Conflict(new { mensaje = $"Ya existe una marca registrada con el nombre '{dto.Nombre}'." });
        }

        var marca = new Marca
        {
            Nombre = dto.Nombre.Trim(),
            PaisOrigen = dto.PaisOrigen.Trim()
        };

        _context.Marcas.Add(marca);
        await _context.SaveChangesAsync();

        var response = new MarcaResponseDto
        {
            Id = marca.Id,
            Nombre = marca.Nombre,
            PaisOrigen = marca.PaisOrigen,
            CantidadModelos = 0
        };

        return CreatedAtAction(nameof(GetMarca), new { id = marca.Id }, response);
    }

    /// <summary>
    /// Actualiza los datos de una marca existente.
    /// </summary>
    [HttpPut("{id}")]
    [SimulatedAuthorize("Admin")]
    public async Task<ActionResult<MarcaResponseDto>> UpdateMarca(int id, [FromBody] ActualizarMarcaDto dto)
    {
        var marca = await _context.Marcas.Include(m => m.Modelos).FirstOrDefaultAsync(m => m.Id == id);
        if (marca == null)
        {
            return NotFound(new { mensaje = $"No se encontró la marca con ID {id}." });
        }

        if (await _context.Marcas.AnyAsync(m => m.Nombre.ToLower() == dto.Nombre.ToLower() && m.Id != id))
        {
            return Conflict(new { mensaje = $"Ya existe otra marca con el nombre '{dto.Nombre}'." });
        }

        marca.Nombre = dto.Nombre.Trim();
        marca.PaisOrigen = dto.PaisOrigen.Trim();

        await _context.SaveChangesAsync();

        return Ok(new MarcaResponseDto
        {
            Id = marca.Id,
            Nombre = marca.Nombre,
            PaisOrigen = marca.PaisOrigen,
            CantidadModelos = marca.Modelos.Count
        });
    }

    /// <summary>
    /// Elimina una marca si no tiene modelos asociados.
    /// </summary>
    [HttpDelete("{id}")]
    [SimulatedAuthorize("Admin")]
    public async Task<IActionResult> DeleteMarca(int id)
    {
        var marca = await _context.Marcas
            .Include(m => m.Modelos)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (marca == null)
        {
            return NotFound(new { mensaje = $"No se encontró la marca con ID {id}." });
        }

        if (marca.Modelos.Any())
        {
            return BadRequest(new { mensaje = $"No se puede eliminar la marca '{marca.Nombre}' porque tiene {marca.Modelos.Count} modelo(s) asociado(s)." });
        }

        _context.Marcas.Remove(marca);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Marca '{marca.Nombre}' eliminada exitosamente." });
    }
}
