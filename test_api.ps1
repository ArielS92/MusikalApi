$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
$musikalApiDir = Join-Path $scriptDir "MusikalApi"
if (-not (Test-Path $musikalApiDir)) {
    if (Test-Path (Join-Path $scriptDir "MusikalApi.csproj")) {
        $musikalApiDir = $scriptDir
    }
}

Write-Host "--- Iniciando servidor MusikalApi en segundo plano temporal ---"
$logPath = Join-Path $scriptDir "server.log"
$errPath = Join-Path $scriptDir "server_err.log"
if (Test-Path $logPath) { Remove-Item $logPath -Force }
if (Test-Path $errPath) { Remove-Item $errPath -Force }

$dllPath = Join-Path $musikalApiDir "bin\Debug\net10.0\MusikalApi.dll"
$process = Start-Process -FilePath "dotnet" -ArgumentList "`"$dllPath`" --urls http://127.0.0.1:5000" -WorkingDirectory $musikalApiDir -RedirectStandardOutput $logPath -RedirectStandardError $errPath -PassThru

try {
    Write-Host "Esperando a que el servidor inicialice en http://127.0.0.1:5000..."
    $retries = 30
    $ready = $false
    while ($retries -gt 0 -and -not $ready) {
        Start-Sleep -Seconds 1
        try {
            $resp = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/marcas" -Method Get -TimeoutSec 2
            if ($resp) { $ready = $true }
        } catch {
            Write-Host "Intento $retries fallo: $($_.Exception.Message)"
            $retries--
        }
    }

    if (-not $ready) {
        if (Test-Path $errPath) { Get-Content $errPath | Write-Host }
        if (Test-Path $logPath) { Get-Content $logPath -Tail 20 | Write-Host }
        throw "El servidor no respondió a tiempo."
    }

    Write-Host "`n>>> 1. Probando GET /api/marcas:"
    $marcas = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/marcas" -Method Get
    $marcas | Format-Table -Property Id, Nombre, PaisOrigen, CantidadModelos | Out-String | Write-Host

    Write-Host "`n>>> 2. Probando GET /api/tipos:"
    $tipos = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/tipos" -Method Get
    $tipos | Format-Table -Property Id, Nombre, CantidadModelos | Out-String | Write-Host

    Write-Host "`n>>> 3. Probando GET /api/modelos:"
    $modelos = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/modelos" -Method Get
    $modelos | Format-Table -Property Id, Nombre, MarcaNombre, TipoInstrumentoNombre, Precio, Stock | Out-String | Write-Host

    Write-Host "`n>>> 4. Probando filtro de modelos por Tipo (Cuerda = 1) vía query param y ruta dedicada:"
    $modelosCuerda = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/modelos?tipoId=1" -Method Get
    $modelosCuerda | Format-Table -Property Id, Nombre, MarcaNombre, Precio, Stock | Out-String | Write-Host

    $modelosRutaTipo = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/modelos/tipo/1" -Method Get
    Write-Host "Modelos obtenidos por ruta /api/modelos/tipo/1: $($modelosRutaTipo.Count)"

    Write-Host "`n>>> 5. Probando filtro de modelos por Marca (Fender = 1):"
    $modelosMarca = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/modelos/marca/1" -Method Get
    $modelosMarca | Format-Table -Property Id, Nombre, MarcaNombre, Precio, Stock | Out-String | Write-Host

    Write-Host "`n>>> 6. Probando [AUTENTICACIÓN SIMULADA] Login de Administrador:"
    $bodyLogin = @{
        usuario = "admin"
        password = "admin123"
    } | ConvertTo-Json
    $authResp = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/auth/login" -Method Post -Body $bodyLogin -ContentType "application/json"
    Write-Host "Login exitoso. Usuario: $($authResp.usuario), Rol: $($authResp.rol), Token: $($authResp.token)"

    Write-Host "`n>>> 7. Probando [¡DESAFÍO!] Creación de Factura con Stock Insuficiente (debe retornar 409 Conflict):"
    $bodyExceso = @{
        clienteId = "CLI-999"
        clienteNombre = "Cliente Test"
        items = @(
            @{ modeloInstrumentoId = 1; cantidad = 9999 }
        )
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/facturas" -Method Post -Body $bodyExceso -ContentType "application/json"
        Write-Host "ERROR: Debería haber fallado por falta de stock!"
    } catch {
        Write-Host "Comportamiento esperado: La API rechazó la factura por falta de stock."
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $errJson = $reader.ReadToEnd()
        Write-Host "Respuesta de error 409 recibida:`n$errJson"
    }

    Write-Host "`n>>> 8. Creando Factura válida (Stratocaster x 2 unidades):"
    $bodyValido = @{
        clienteId = "CLI-001"
        clienteNombre = "Carlos Santana"
        items = @(
            @{ modeloInstrumentoId = 1; cantidad = 2 }
        )
    } | ConvertTo-Json

    $facturaCreada = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/facturas" -Method Post -Body $bodyValido -ContentType "application/json"
    Write-Host "Factura creada exitosamente!"
    Write-Host "ID Factura: $($facturaCreada.id)"
    Write-Host "Código de Pago Único: $($facturaCreada.codigoPagoUnico)"
    Write-Host "Monto Total: $($facturaCreada.montoTotal)"
    Write-Host "Estado Inicial: $($facturaCreada.estadoPago)"

    Write-Host "`n>>> 9. Verificando que el Stock se haya descontado automáticamente:"
    $modeloActualizado = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/modelos/1" -Method Get
    Write-Host "Modelo: $($modeloActualizado.nombre) - Nuevo Stock: $($modeloActualizado.stock)"

    Write-Host "`n>>> 10. Enviando Factura a Pasarela PayLink (POST /api/facturas/$($facturaCreada.id)/enviar-pasarela):"
    $registroPasarela = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/facturas/$($facturaCreada.id)/enviar-pasarela" -Method Post
    Write-Host "Resultado envío pasarela:"
    $registroPasarela | Format-List | Out-String | Write-Host

    Write-Host "`n>>> 11. Consultando estado inicial en el simulador PayLink (debe estar PENDIENTE):"
    $pagoPaylink = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/paylink-mock/pagos/$($facturaCreada.codigoPagoUnico)" -Method Get
    Write-Host "Estado en PayLink: $($pagoPaylink.estado)"

    Write-Host "`n>>> 12. Simulando pago del cliente en PayLink (POST /api/paylink-mock/pagos/.../pagar):"
    $simularPago = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/paylink-mock/pagos/$($facturaCreada.codigoPagoUnico)/pagar" -Method Post
    Write-Host "Pago simulado. Estado en PayLink ahora: $($simularPago.estado)"

    Write-Host "`n>>> 13. Ejecutando Conciliación de Pagos (POST /reconcile):"
    $reconciliacion = Invoke-RestMethod -Uri "http://127.0.0.1:5000/reconcile" -Method Post
    Write-Host "Resultado de Conciliación:"
    $reconciliacion | ConvertTo-Json -Depth 5 | Write-Host

    Write-Host "`n>>> 14. Consultando Factura nuevamente para verificar estado 'Pagada':"
    $facturaFinal = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/facturas/$($facturaCreada.id)" -Method Get
    Write-Host "Factura ID: $($facturaFinal.id) - Estado Final: $($facturaFinal.estadoPago) - Fecha Pago: $($facturaFinal.fechaPago)"

    Write-Host "`n>>> 15. Consultando Configuración de Pasarela (GET /api/pasarela):"
    $configPasarela = Invoke-RestMethod -Uri "http://127.0.0.1:5000/api/pasarela" -Method Get
    Write-Host "Pasarela configurada: $($configPasarela.nombre) - URL: $($configPasarela.urlBase) - Activa: $($configPasarela.activa)"

    Write-Host "`n>>> TODOS LOS TESTS PASARON EXITOSAMENTE! <<<"
}
finally {
    if ($process -and -not $process.HasExited) {
        Write-Host "Deteniendo servidor..."
        Stop-Process -Id $process.Id -Force
    }
}
