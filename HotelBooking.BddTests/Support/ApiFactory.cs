using HotelBooking.WebApi;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotelBooking.BddTests.Support;

public class ApiFactory : WebApplicationFactory<Program>
{
    // We rely on Program.cs seeding in Development. No overrides needed.
    // Program.cs already uses InMemory DB and calls DbInitializer in Development.  contentReference[oaicite:13]{index=13}
}
