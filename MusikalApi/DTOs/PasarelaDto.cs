using System.ComponentModel.DataAnnotations;

namespace MusikalApi.DTOs;

public class ConfiguracionPasarelaDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Url(ErrorMessage = "La URL base de la pasarela debe ser una URL válida.")]
    public string UrlBase { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string ApiKey { get; set; } = string.Empty;

    public bool Activa { get; set; }
}

public class RegistroPagoPasarelaRequest
{
    [Required]
    public string CodigoPago { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}

public class RegistroPagoPasarelaResponse
{
    public bool Exitoso { get; set; }
    public string CodigoPago { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Estado { get; set; }
}

public class ConsultaPagoPasarelaResponse
{
    public string CodigoPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Estado { get; set; } = string.Empty; // PENDIENTE, PAGADO, RECHAZADO
    public DateTime? FechaPago { get; set; }
    public string? Mensaje { get; set; }
}

public class DetalleConciliacionItemDto
{
    public int FacturaId { get; set; }
    public string CodigoPagoUnico { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string EstadoEnPasarela { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
}

public class ResultadoConciliacionDto
{
    public DateTime FechaEjecucion { get; set; } = DateTime.UtcNow;
    public int TotalFacturasPendientesRevisadas { get; set; }
    public int FacturasConciliadasPagadas { get; set; }
    public int FacturasAunPendientes { get; set; }
    public int FacturasConErrorConsulta { get; set; }
    public List<DetalleConciliacionItemDto> Detalles { get; set; } = new();
}
