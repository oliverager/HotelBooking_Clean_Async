using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using HotelBooking.BddTests.Support;
using Reqnroll;

namespace HotelBooking.BddTests.Steps;

[Binding]
public class CreateBookingSteps
{
    private readonly DateContext date;
    private readonly TestContext ctx;

    static DateTime OnlyDate(DateTime dt) => dt.Date;

    
    public CreateBookingSteps(TestContext ctx, DateContext date)
    {
        this.ctx = ctx;
        this.date = date;
    }

    [Given(@"the database is freshly seeded")]
    public void GivenTheDatabaseIsFreshlySeeded()
    {
        // Seeding is performed in Program.cs when the app starts in Development. :contentReference[oaicite:14]{index=14}
        // Nothing to do here.
    }

    [When(@"I create a booking from day \+(\d+) to day \+(\d+) for customer (\d+)")]
    public async Task WhenICreateABooking(int startOffset, int endOffset, int customerId)
    {
        var body = new {
            startDate = date.D(startOffset),
            endDate   = date.D(endOffset),
            customerId
        };
        ctx.LastResponse = await ctx.Client.PostAsJsonAsync("/bookings", body);
    }
    
    // mark it as regex explicitly
    [Given(@"regex:^today is fixed to (\d{4})-(\d{2})-(\d{2})$")]
    public void FixToday_Regex(int y, int m, int d)
    {
        date.Today = new DateTime(y, m, d);
    }

    [When(@"I submit an empty request to create a booking")]
    public async Task WhenISubmitEmptyRequest()
    {
        // Send an empty JSON payload with content-type so the API returns 400 (Bad Request)
        // instead of 415 (Unsupported Media Type).
        var emptyJson = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        ctx.LastResponse = await ctx.Client.PostAsync("/bookings", emptyJson);
    }

    [Then(@"the HTTP status should be (\d+)")]
    public async Task ThenTheHttpStatusShouldBe(int expected)
    {
        ctx.LastResponse.Should().NotBeNull();
        ((int)ctx.LastResponse!.StatusCode).Should().Be(expected); 
        _ = await ctx.LastResponse.Content.ReadAsStringAsync(); // materialize for debug
    }
}