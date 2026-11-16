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
public class ModelosController : ControllerBase
{
    private readonly MusikalDbContext _context;

    public ModelosController(MusikalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista y busca modelos de instrumentos, con filtros opcionales por Tipo, Marca y búsqueda por texto.
    /// </summary>
    /// <param name="marcaId">ID opcional de la marca para filtrar.</param>
    /// <param name="tipoId">ID opcional del tipo de instrumento para filtrar.</param>
    /// <param name="buscar">Término de búsqueda opcional sobre el nombre o descripción.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModeloInstrumentoResponseDto>>> GetModelos(
        [FromQuery] int? marcaId,
        [FromQuery] int? tipoId,
        [FromQuery] string? buscar)
    {
        var query = _context.Modelos
            .Include(m => m.Marca)
            .Include(m => m.TipoInstrumento)
            .AsQueryable();

        if (marcaId.HasValue)
        {
            query = query.Where(m => m.MarcaId == marcaId.Value);
        }

        if (tipoId.HasValue)
        {
            query = query.Where(m => m.TipoInstrumentoId == tipoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLower();
            query = query.Where(m => m.Nombre.ToLower().Contains(term) || m.Descripcion.ToLower().Contains(term));
        }

        var modelos = await query
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
    /// Lista los modelos de instrumentos filtrados por Tipo de Instrumento.
    /// </summary>
    [HttpGet("tipo/{tipoId}")]
    public async Task<ActionResult<IEnumerable<ModeloInstrumentoResponseDto>>> GetModelosPorTipo(int tipoId)
    {
        return await GetModelos(marcaId: null, tipoId: tipoId, buscar: null);
    }

    /// <summary>
    /// Lista los modelos de instrumentos filtrados por Marca.
    /// </summary>
    [HttpGet("marca/{marcaId}")]
    public async Task<ActionResult<IEnumerable<ModeloInstrumentoResponseDto>>> GetModelosPorMarca(int marcaId)
    {
        return await GetModelos(marcaId: marcaId, tipoId: null, buscar: null);
    }

    /// <summary>
    /// Obtiene un modelo específico por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ModeloInstrumentoResponseDto>> GetModelo(int id)
    {
        var modelo = await _context.Modelos
            .Include(m => m.Marca)
            .Include(m => m.TipoInstrumento)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (modelo == null)
        {
            return NotFound(new { mensaje = $"No se encontró el modelo con ID {id}." });
        }

        return Ok(new ModeloInstrumentoResponseDto
        {
            Id = modelo.Id,
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            MarcaId = modelo.MarcaId,
            MarcaNombre = modelo.Marca != null ? modelo.Marca.Nombre : string.Empty,
            TipoInstrumentoId = modelo.TipoInstrumentoId,
            TipoInstrumentoNombre = modelo.TipoInstrumento != null ? modelo.TipoInstrumento.Nombre : string.Empty,
            Precio = modelo.Precio,
            Stock = modelo.Stock
        });
    }

    /// <summary>
    /// Registra un nuevo modelo de instrumento.
    /// </summary>
    [HttpPost]
    [SimulatedAuthorize("Admin")]
    public async Task<ActionResult<ModeloInstrumentoResponseDto>> CreateModelo([FromBody] CrearModeloInstrumentoDto dto)
    {
        var marca = await _context.Marcas.FindAsync(dto.MarcaId);
        if (marca == null)
        {
            return BadRequest(new { mensaje = $"La marca con ID {dto.MarcaId} no existe." });
        }

        var tipo = await _context.TiposInstrumento.FindAsync(dto.TipoInstrumentoId);
        if (tipo == null)
        {
            return BadRequest(new { mensaje = $"El tipo de instrumento con ID {dto.TipoInstrumentoId} no existe." });
        }

        var modelo = new ModeloInstrumento
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
            MarcaId = dto.MarcaId,
            TipoInstrumentoId = dto.TipoInstrumentoId,
            Precio = dto.Precio,
            Stock = dto.Stock
        };

        _context.Modelos.Add(modelo);
        await _context.SaveChangesAsync();

        var response = new ModeloInstrumentoResponseDto
        {
            Id = modelo.Id,
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            MarcaId = marca.Id,
            MarcaNombre = marca.Nombre,
            TipoInstrumentoId = tipo.Id,
            TipoInstrumentoNombre = tipo.Nombre,
            Precio = modelo.Precio,
            Stock = modelo.Stock
        };

        return CreatedAtAction(nameof(GetModelo), new { id = modelo.Id }, response);
    }

    /// <summary>
    /// Actualiza los datos de un modelo existente.
    /// </summary>
    [HttpPut("{id}")]
    [SimulatedAuthorize("Admin")]
    public async Task<ActionResult<ModeloInstrumentoResponseDto>> UpdateModelo(int id, [FromBody] ActualizarModeloInstrumentoDto dto)
    {
        var modelo = await _context.Modelos.FindAsync(id);
        if (modelo == null)
        {
            return NotFound(new { mensaje = $"No se encontró el modelo con ID {id}." });
        }

        var marca = await _context.Marcas.FindAsync(dto.MarcaId);
        if (marca == null)
        {
            return BadRequest(new { mensaje = $"La marca con ID {dto.MarcaId} no existe." });
        }

        var tipo = await _context.TiposInstrumento.FindAsync(dto.TipoInstrumentoId);
        if (tipo == null)
        {
            return BadRequest(new { mensaje = $"El tipo de instrumento con ID {dto.TipoInstrumentoId} no existe." });
        }

        modelo.Nombre = dto.Nombre.Trim();
        modelo.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
        modelo.MarcaId = dto.MarcaId;
        modelo.TipoInstrumentoId = dto.TipoInstrumentoId;
        modelo.Precio = dto.Precio;
        modelo.Stock = dto.Stock;

        await _context.SaveChangesAsync();

        return Ok(new ModeloInstrumentoResponseDto
        {
            Id = modelo.Id,
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            MarcaId = marca.Id,
            MarcaNombre = marca.Nombre,
            TipoInstrumentoId = tipo.Id,
            TipoInstrumentoNombre = tipo.Nombre,
            Precio = modelo.Precio,
            Stock = modelo.Stock
        });
    }

    /// <summary>
    /// Elimina un modelo de instrumento si no está incluido en facturas.
    /// </summary>
    [HttpDelete("{id}")]
    [SimulatedAuthorize("Admin")]
    public async Task<IActionResult> DeleteModelo(int id)
    {
        var modelo = await _context.Modelos.FindAsync(id);
        if (modelo == null)
        {
            return NotFound(new { mensaje = $"No se encontró el modelo con ID {id}." });
        }

        var tieneFacturas = await _context.FacturaDetalles.AnyAsync(d => d.ModeloInstrumentoId == id);
        if (tieneFacturas)
        {
            return BadRequest(new { mensaje = $"No se puede eliminar el modelo '{modelo.Nombre}' porque ya forma parte del historial de facturación." });
        }

        _context.Modelos.Remove(modelo);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Modelo '{modelo.Nombre}' eliminado exitosamente." });
    }
}
