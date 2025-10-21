using FluentAssertions;
using HotelBooking.BddTests.Support;
using HotelBooking.Core;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace HotelBooking.BddTests.Steps;

[Binding]
public class ManagerSteps
{
    private readonly DateContext date;
    private readonly TestContext ctx;
    private Exception? captured;
    private List<DateTime>? dates;

    public ManagerSteps(TestContext ctx, DateContext date)
    {
        this.ctx = ctx;
        this.date = date;
    }

    [When(@"I ask the manager for an available room from day \+(\d+) to day \+(\d+)")]
    public async Task WhenIAskTheManager(int s, int e)
    {
        try
        {
            using var scope = ctx.Factory.Services.CreateScope();
            var mgr = scope.ServiceProvider.GetRequiredService<IBookingManager>();

            await mgr.FindAvailableRoom(this.date.D(s), this.date.D(e)); // throws on invalid input
        }
        catch (Exception ex)
        {
            captured = ex;
        }
    }

    [Then(@"an argument error is thrown")]
    public void ThenAnArgumentErrorIsThrown()
        => captured.Should().BeOfType<ArgumentException>();

    [When(@"I ask for fully occupied dates from day \+(\d+) to day \+(\d+)")]
    public async Task WhenIAskForFullyOccupiedDates(int s, int e)
    {
        using var scope = ctx.Factory.Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IBookingManager>();

        dates = await mgr.GetFullyOccupiedDates(this.date.D(s), this.date.D(e));
    }

    [Then(@"the result should contain every date from day \+(\d+) to day \+(\d+)")]
    public void ThenContainsRange(int s, int e)
    {
        dates.Should().NotBeNull();
        var expected = Enumerable.Range(s, e - s + 1).Select(this.date.D);
        dates!.Should().Contain(expected);
    }

    [Then(@"the result should contain exactly (\d+) dates")]
    public void ThenCountIs(int n) => dates!.Count.Should().Be(n);
}
