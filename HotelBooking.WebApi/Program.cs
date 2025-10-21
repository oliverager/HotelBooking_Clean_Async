using System.Net;
using HotelBooking.Core;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
//using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//SqliteConnection connection = new SqliteConnection("DataSource=:memory:");
// In-memory database only exists while the connection is open
//connection.Open();
builder.Services.AddDbContext<HotelBookingContext>(opt => 
    opt.UseInMemoryDatabase("HotelBookingDb"));

builder.Services.AddScoped<IRepository<Room>, RoomRepository>();
builder.Services.AddScoped<IRepository<Customer>, CustomerRepository>();
builder.Services.AddScoped<IRepository<Booking>, BookingRepository>();
builder.Services.AddScoped<IBookingManager, BookingManager>();
builder.Services.AddTransient<IDbInitializer, DbInitializer>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    seeder.Initialize(db);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}