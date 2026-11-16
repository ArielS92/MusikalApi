using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MusikalApi.Data;
using MusikalApi.DTOs;

namespace MusikalApi.Services;

public class PayLinkService : IPasarelaPagosService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayLinkService> _logger;

    public PayLinkService(
        HttpClient httpClient,
        IServiceProvider serviceProvider,
        ILogger<PayLinkService> logger)
    {
        _httpClient = httpClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private async Task<(string UrlBase, string ApiKey)> ObtenerConfiguracionAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MusikalDbContext>();
        var config = await db.ConfiguracionesPasarela
            .FirstOrDefaultAsync(c => c.Activa) 
            ?? new Models.ConfiguracionPasarela();

        return (config.UrlBase.TrimEnd('/'), config.ApiKey);
    }

    public async Task<RegistroPagoPasarelaResponse> RegistrarPagoAsync(
        string codigoPago,
        decimal monto,
        string descripcion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (urlBase, apiKey) = await ObtenerConfiguracionAsync();
            var targetUrl = $"{urlBase}/pagos";

            _logger.LogInformation("Enviando factura/pago a PayLink en {Url}. Código: {Codigo}, Monto: {Monto}",
                targetUrl, codigoPago, monto);

            var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = JsonContent.Create(new RegistroPagoPasarelaRequest
                {
                    CodigoPago = codigoPago,
                    Monto = monto,
                    Descripcion = descripcion
                })
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegistroPagoPasarelaResponse>(cancellationToken: cancellationToken);
                return result ?? new RegistroPagoPasarelaResponse
                {
                    Exitoso = true,
                    CodigoPago = codigoPago,
                    Mensaje = "Pago registrado exitosamente en la pasarela PayLink."
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Error al registrar pago en PayLink. Status: {Status}, Body: {Body}",
                response.StatusCode, errorBody);

            return new RegistroPagoPasarelaResponse
            {
                Exitoso = false,
                CodigoPago = codigoPago,
                Mensaje = $"Error de comunicación con PayLink (HTTP {(int)response.StatusCode}): {errorBody}"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al conectar con PayLink.");
            return new RegistroPagoPasarelaResponse
            {
                Exitoso = false,
                CodigoPago = codigoPago,
                Mensaje = $"No se pudo establecer conexión con el servidor de PayLink: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada al conectar con PayLink.");
            return new RegistroPagoPasarelaResponse
            {
                Exitoso = false,
                CodigoPago = codigoPago,
                Mensaje = $"Error inesperado al conectar con la pasarela: {ex.Message}"
            };
        }
    }

    public async Task<ConsultaPagoPasarelaResponse?> ConsultarEstadoPagoAsync(
        string codigoPago,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (urlBase, apiKey) = await ObtenerConfiguracionAsync();
            var targetUrl = $"{urlBase}/pagos/{Uri.EscapeDataString(codigoPago)}";

            _logger.LogInformation("Consultando estado de pago en PayLink: {Url}", targetUrl);

            var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("El pago {Codigo} no existe en PayLink.", codigoPago);
                return new ConsultaPagoPasarelaResponse
                {
                    CodigoPago = codigoPago,
                    Estado = "NO_ENCONTRADO",
                    Mensaje = "El pago no se encuentra registrado en PayLink."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error consultando PayLink para {Codigo}: HTTP {Status} - {Error}",
                    codigoPago, response.StatusCode, errorContent);
                return new ConsultaPagoPasarelaResponse
                {
                    CodigoPago = codigoPago,
                    Estado = "ERROR_CONSULTA",
                    Mensaje = $"Error al consultar PayLink (HTTP {(int)response.StatusCode}): {errorContent}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<ConsultaPagoPasarelaResponse>(cancellationToken: cancellationToken);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al consultar pago {Codigo} en PayLink.", codigoPago);
            return new ConsultaPagoPasarelaResponse
            {
                CodigoPago = codigoPago,
                Estado = "ERROR_CONEXION",
                Mensaje = $"Fallo de conexión con PayLink: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada al consultar pago {Codigo} en PayLink.", codigoPago);
            return new ConsultaPagoPasarelaResponse
            {
                CodigoPago = codigoPago,
                Estado = "ERROR_INTERNO",
                Mensaje = $"Error inesperado: {ex.Message}"
            };
        }
    }
}
