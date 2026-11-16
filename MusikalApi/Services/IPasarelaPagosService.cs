using MusikalApi.DTOs;

namespace MusikalApi.Services;

public interface IPasarelaPagosService
{
    Task<RegistroPagoPasarelaResponse> RegistrarPagoAsync(string codigoPago, decimal monto, string descripcion, CancellationToken cancellationToken = default);
    Task<ConsultaPagoPasarelaResponse?> ConsultarEstadoPagoAsync(string codigoPago, CancellationToken cancellationToken = default);
}
