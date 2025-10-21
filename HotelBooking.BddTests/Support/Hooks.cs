using Reqnroll;

namespace HotelBooking.BddTests.Support;

[Binding]
public sealed class Hooks
{
    private readonly TestContext ctx;

    public Hooks(TestContext ctx) => this.ctx = ctx;

    [BeforeScenario]
    public void StartServer()
    {
        ctx.Factory = new ApiFactory();
        ctx.Client  = ctx.Factory.CreateClient(); // env=Development => DB seeded
    }

    [AfterScenario]
    public void StopServer()
    {
        ctx.Client?.Dispose();
        ctx.Factory?.Dispose();
    }
}