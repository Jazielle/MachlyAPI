using MongoDB.Driver;
using MachlyAPI.Models;

namespace MachlyAPI.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var databaseName = configuration["MongoDb:DatabaseName"];

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        // Crear índices al inicializar
        CreateIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Machine> Machines => _database.GetCollection<Machine>("machines");
    public IMongoCollection<Booking> Bookings => _database.GetCollection<Booking>("bookings");
    public IMongoCollection<Review> Reviews => _database.GetCollection<Review>("reviews");
    public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("notifications");

    private void CreateIndexes()
    {
        // Índice único en email de usuarios
        var userEmailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
        Users.Indexes.CreateOne(new CreateIndexModel<User>(userEmailIndex, new CreateIndexOptions { Unique = true }));

        // Índice geoespacial 2dsphere en ubicación de máquinas
        var machineLocationIndex = Builders<Machine>.IndexKeys.Geo2DSphere(m => m.Location);
        Machines.Indexes.CreateOne(new CreateIndexModel<Machine>(machineLocationIndex));

        // Índices en bookings
        var bookingMachineIndex = Builders<Booking>.IndexKeys.Ascending(b => b.MachineId);
        Bookings.Indexes.CreateOne(new CreateIndexModel<Booking>(bookingMachineIndex));

        var bookingRenterIndex = Builders<Booking>.IndexKeys.Ascending(b => b.RenterId);
        Bookings.Indexes.CreateOne(new CreateIndexModel<Booking>(bookingRenterIndex));

        var bookingProviderIndex = Builders<Booking>.IndexKeys.Ascending(b => b.ProviderId);
        Bookings.Indexes.CreateOne(new CreateIndexModel<Booking>(bookingProviderIndex));

        // Índice en reviews
        var reviewMachineIndex = Builders<Review>.IndexKeys.Ascending(r => r.MachineId);
        Reviews.Indexes.CreateOne(new CreateIndexModel<Review>(reviewMachineIndex));
    }
}
