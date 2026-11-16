using Microsoft.EntityFrameworkCore;
using MusikalApi.Models;

namespace MusikalApi.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(MusikalDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Marcas.AnyAsync())
        {
            var marcas = new List<Marca>
            {
                new() { Nombre = "Fender", PaisOrigen = "Estados Unidos" },
                new() { Nombre = "Gibson", PaisOrigen = "Estados Unidos" },
                new() { Nombre = "Yamaha", PaisOrigen = "Japón" },
                new() { Nombre = "Roland", PaisOrigen = "Japón" },
                new() { Nombre = "Shure", PaisOrigen = "Estados Unidos" }
            };
            await context.Marcas.AddRangeAsync(marcas);
            await context.SaveChangesAsync();
        }

        if (!await context.TiposInstrumento.AnyAsync())
        {
            var tipos = new List<TipoInstrumento>
            {
                new() { Nombre = "Cuerda" },
                new() { Nombre = "Percusión" },
                new() { Nombre = "Viento" },
                new() { Nombre = "Teclado" },
                new() { Nombre = "Audio Profesional" }
            };
            await context.TiposInstrumento.AddRangeAsync(tipos);
            await context.SaveChangesAsync();
        }

        if (!await context.Modelos.AnyAsync())
        {
            var fender = await context.Marcas.FirstAsync(m => m.Nombre == "Fender");
            var gibson = await context.Marcas.FirstAsync(m => m.Nombre == "Gibson");
            var yamaha = await context.Marcas.FirstAsync(m => m.Nombre == "Yamaha");
            var roland = await context.Marcas.FirstAsync(m => m.Nombre == "Roland");
            var shure = await context.Marcas.FirstAsync(m => m.Nombre == "Shure");

            var cuerda = await context.TiposInstrumento.FirstAsync(t => t.Nombre == "Cuerda");
            var percusion = await context.TiposInstrumento.FirstAsync(t => t.Nombre == "Percusión");
            var viento = await context.TiposInstrumento.FirstAsync(t => t.Nombre == "Viento");
            var teclado = await context.TiposInstrumento.FirstAsync(t => t.Nombre == "Teclado");
            var audio = await context.TiposInstrumento.FirstAsync(t => t.Nombre == "Audio Profesional");

            var modelos = new List<ModeloInstrumento>
            {
                new()
                {
                    Nombre = "Stratocaster Player Series",
                    Descripcion = "Guitarra eléctrica con 3 pastillas single-coil y diapasón de maple.",
                    MarcaId = fender.Id,
                    TipoInstrumentoId = cuerda.Id,
                    Precio = 850.00m,
                    Stock = 10
                },
                new()
                {
                    Nombre = "Telecaster Classic Vibe",
                    Descripcion = "Guitarra eléctrica clásica con tono brillante y cuerpo de aliso.",
                    MarcaId = fender.Id,
                    TipoInstrumentoId = cuerda.Id,
                    Precio = 720.00m,
                    Stock = 7
                },
                new()
                {
                    Nombre = "Les Paul Standard 60s",
                    Descripcion = "Guitarra eléctrica con tapas de arce flameado y pastillas humbucker Burstbucker.",
                    MarcaId = gibson.Id,
                    TipoInstrumentoId = cuerda.Id,
                    Precio = 2499.00m,
                    Stock = 4
                },
                new()
                {
                    Nombre = "Stage Custom Birch",
                    Descripcion = "Batería acústica completa de abedul de 5 cuerpos.",
                    MarcaId = yamaha.Id,
                    TipoInstrumentoId = percusion.Id,
                    Precio = 1250.00m,
                    Stock = 3
                },
                new()
                {
                    Nombre = "YFL-222 Flauta Traversa",
                    Descripcion = "Flauta traversa de estudio niquelada afinada en Do.",
                    MarcaId = yamaha.Id,
                    TipoInstrumentoId = viento.Id,
                    Precio = 480.00m,
                    Stock = 6
                },
                new()
                {
                    Nombre = "RD-88 Stage Piano",
                    Descripcion = "Piano digital de 88 teclas con acción martillo PHA-4 y motor SuperNATURAL.",
                    MarcaId = roland.Id,
                    TipoInstrumentoId = teclado.Id,
                    Precio = 1390.00m,
                    Stock = 5
                },
                new()
                {
                    Nombre = "SM58 Micrófono Vocal",
                    Descripcion = "Micrófono dinámico cardioide legendario para voces en vivo.",
                    MarcaId = shure.Id,
                    TipoInstrumentoId = audio.Id,
                    Precio = 110.00m,
                    Stock = 20
                }
            };
            await context.Modelos.AddRangeAsync(modelos);
            await context.SaveChangesAsync();
        }

        if (!await context.ConfiguracionesPasarela.AnyAsync())
        {
            var configuracion = new ConfiguracionPasarela
            {
                Nombre = "PayLink",
                UrlBase = "http://localhost:5000/api/paylink-mock",
                ApiKey = "paylink_live_secret_key_musikal_2026",
                Activa = true
            };
            await context.ConfiguracionesPasarela.AddAsync(configuracion);
            await context.SaveChangesAsync();
        }
    }
}
