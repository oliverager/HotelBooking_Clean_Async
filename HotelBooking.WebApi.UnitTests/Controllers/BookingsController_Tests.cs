using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelBooking.WebApi.UnitTests
{
    public class BookingsController_Tests
    {
        [Fact]
        public async Task Post_Created_WhenManagerCreatesBooking()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var manager = new Mock<IBookingManager>();
            manager.Setup(m => m.CreateBooking(It.IsAny<Booking>()))
                   .ReturnsAsync(true);

            var sut = new BookingsController(bookingRepo.Object, manager.Object);

            var result = await sut.Post(new Booking());

            var created = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal("GetBookings", created.RouteName); // as implemented
        }

        [Fact]
        public async Task Post_Conflict_WhenManagerReturnsFalse()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var manager = new Mock<IBookingManager>();
            manager.Setup(m => m.CreateBooking(It.IsAny<Booking>()))
                   .ReturnsAsync(false);

            var sut = new BookingsController(bookingRepo.Object, manager.Object);

            var result = await sut.Post(new Booking());

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }
    }
}
