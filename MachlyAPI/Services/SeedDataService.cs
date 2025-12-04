using MongoDB.Driver;
using MachlyAPI.Models;
using MachlyAPI.Models.Enums;
using BCrypt.Net;

namespace MachlyAPI.Services;

public class SeedDataService
{
    private readonly MongoDbService _mongoDb;

    public SeedDataService(MongoDbService mongoDb)
    {
        _mongoDb = mongoDb;
    }

    public async Task SeedAsync()
    {
        // Verificar si ya existen usuarios
        var userCount = await _mongoDb.Users.CountDocumentsAsync(_ => true);
        if (userCount > 0)
        {
            return; // Ya hay datos, no hacer nada
        }

        // 1. Crear Usuarios
        var admin = new User
        {
            Name = "Admin",
            Lastname = "Machly",
            Email = "admin@machly.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.ADMIN,
            Verified = true,
            Phone = "70000000",
            CreatedAt = DateTime.UtcNow
        };

        var provider1 = new User
        {
            Name = "Juan",
            Lastname = "Perez",
            Email = "juan.perez@machly.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Provider123!"),
            Role = UserRole.PROVIDER,
            Verified = true,
            Phone = "71111111",
            CreatedAt = DateTime.UtcNow
        };

        var provider2 = new User
        {
            Name = "Maria",
            Lastname = "Gomez",
            Email = "maria.gomez@machly.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Provider123!"),
            Role = UserRole.PROVIDER,
            Verified = true,
            Phone = "72222222",
            CreatedAt = DateTime.UtcNow
        };

        var renter = new User
        {
            Name = "Carlos",
            Lastname = "Lopez",
            Email = "carlos.renter@machly.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Renter123!"),
            Role = UserRole.RENTER,
            Verified = true,
            Phone = "73333333",
            CreatedAt = DateTime.UtcNow
        };

        await _mongoDb.Users.InsertManyAsync(new[] { admin, provider1, provider2, renter });

        // 2. Crear Máquinas
        var machines = new List<Machine>
        {
            new Machine
            {
                ProviderId = provider1.Id!,
                Title = "Tractor John Deere 5075E",
                Description = "Tractor agrícola de 75 HP, ideal para trabajos de preparación de suelo y siembra. Incluye operador con experiencia.",
                Category = MachineCategory.Servicios,
                CategoryData = new CategoryData
                {
                    TarifaBase = 50, // USD por hora o hectárea
                    WithOperator = true,
                    Hectareas = 0, // Capacidad/Rendimiento
                    TarifaOperador = 10
                },
                Location = new GeoLocation { Latitude = -17.7833, Longitude = -63.1821 }, // Santa Cruz de la Sierra
                Photos = new List<string> { "https://placehold.co/600x400/png?text=Tractor+JD" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Machine
            {
                ProviderId = provider1.Id!,
                Title = "Cosechadora New Holland TC5.90",
                Description = "Cosechadora de alta capacidad para granos. Eficiencia garantizada en soya y maíz.",
                Category = MachineCategory.Servicios,
                CategoryData = new CategoryData
                {
                    TarifaBase = 120,
                    WithOperator = true,
                    Hectareas = 0,
                    TarifaOperador = 20
                },
                Location = new GeoLocation { Latitude = -17.3935, Longitude = -63.2515 }, // Montero
                Photos = new List<string> { "https://placehold.co/600x400/png?text=Cosechadora+NH" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Machine
            {
                ProviderId = provider2.Id!,
                Title = "Sembradora Semeato",
                Description = "Sembradora de precisión para siembra directa. 12 líneas.",
                Category = MachineCategory.Semillas, // Usando categoría Semillas como proxy para maquinaria de siembra si no hay categoría específica
                CategoryData = new CategoryData
                {
                    TarifaBase = 40,
                    WithOperator = false,
                    Hectareas = 0
                },
                Location = new GeoLocation { Latitude = -17.2300, Longitude = -63.0500 }, // Warnes
                Photos = new List<string> { "https://placehold.co/600x400/png?text=Sembradora" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Machine
            {
                ProviderId = provider2.Id!,
                Title = "Camión Volvo FH16",
                Description = "Camión de alto tonelaje para transporte de cosecha. Capacidad 30 toneladas.",
                Category = MachineCategory.Caña, // Usando Caña como proxy para transporte de caña/granos
                CategoryData = new CategoryData
                {
                    TarifaBase = 200,
                    WithOperator = true,
                    Toneladas = 30,
                    Kilometros = 0
                },
                Location = new GeoLocation { Latitude = -17.8000, Longitude = -63.1000 }, // Cotoca
                Photos = new List<string> { "https://placehold.co/600x400/png?text=Camion+Volvo" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Machine
            {
                ProviderId = provider1.Id!,
                Title = "Fumigadora Autopropulsada",
                Description = "Pulverizadora con barras de 30 metros. Control por GPS.",
                Category = MachineCategory.Servicios,
                CategoryData = new CategoryData
                {
                    TarifaBase = 80,
                    WithOperator = true,
                    Hectareas = 0
                },
                Location = new GeoLocation { Latitude = -17.5000, Longitude = -63.3000 }, // Mineros
                Photos = new List<string> { "https://placehold.co/600x400/png?text=Fumigadora" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _mongoDb.Machines.InsertManyAsync(machines);
    }
}
