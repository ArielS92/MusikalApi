using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MusikalApi.Data;
using MusikalApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la Base de Datos SQLite
builder.Services.AddDbContext<MusikalDbContext>(options =>
{
    var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                           ?? "Data Source=musikal.db";

    // Si la ruta del archivo SQLite es relativa, anclarla siempre a ContentRootPath (carpeta de la API)
    if (rawConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        var dbPath = rawConnectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!Path.IsPathRooted(dbPath))
        {
            var absoluteDbPath = Path.Combine(builder.Environment.ContentRootPath, dbPath);
            rawConnectionString = $"Data Source={absoluteDbPath}";
        }
    }

    options.UseSqlite(rawConnectionString);
});

// Configuración de Controladores y Serialización JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Inyección de HttpClient para la pasarela de pagos
builder.Services.AddHttpClient<IPasarelaPagosService, PayLinkService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Configuración de Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Musikal API - Tienda de Instrumentos Musicales",
        Version = "v1",
        Description = "API RESTful para la gestión del catálogo de instrumentos musicales (Marcas, Tipos, Modelos), " +
                      "flujo de facturación con control atómico de stock, y conciliación con la pasarela de pagos 'PayLink'.",
        Contact = new OpenApiContact
        {
            Name = "Equipo Musikal API",
            Email = "contacto@musikal.local"
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Autenticación simulada: ingrese 'Bearer {token}' (obtenido en /api/auth/login) o utilice el header 'X-User-Role: Admin|Vendedor'."
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

var app = builder.Build();

// Inicialización y Seeding de la Base de Datos
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MusikalDbContext>();
    await DbInitializer.SeedAsync(db);
}

// Configuración del pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Musikal API v1");
    c.RoutePrefix = string.Empty; // Permite abrir Swagger directamente en la raíz http://localhost:5000/
});

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
