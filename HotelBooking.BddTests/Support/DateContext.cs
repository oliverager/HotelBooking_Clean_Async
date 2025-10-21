namespace HotelBooking.BddTests.Support;

public class DateContext
{
    public DateTime Today { get; set; } = DateTime.Today; 
    public DateTime D(int offsetDays) => Today.AddDays(offsetDays).Date;
}