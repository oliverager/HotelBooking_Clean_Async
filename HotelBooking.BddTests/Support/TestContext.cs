namespace HotelBooking.BddTests.Support;

public class TestContext
{
    public ApiFactory Factory { get; set; } = default!;
    public HttpClient Client { get; set; } = default!;
    public HttpResponseMessage? LastResponse { get; set; }
}