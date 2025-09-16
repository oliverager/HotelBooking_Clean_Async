#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManager_MoqTests
    {
        private readonly Mock<IRepository<Booking>> _bookingRepo = new();
        private readonly Mock<IRepository<Room>> _roomRepo = new();

        private BookingManager CreateSut(
            IEnumerable<Room>? rooms = null,
            IEnumerable<Booking>? bookings = null)
        {
            _roomRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(rooms ?? new[] {
                         new Room{ Id = 1, Description = "A"},
                         new Room{ Id = 2, Description = "B"},
                         new Room{ Id = 3, Description = "C"},
                     });

            _bookingRepo.Setup(b => b.GetAllAsync())
                        .ReturnsAsync(bookings ?? Array.Empty<Booking>());

            _bookingRepo.Setup(b => b.AddAsync(It.IsAny<Booking>()))
                        .Returns(Task.CompletedTask);

            return new BookingManager(_bookingRepo.Object, _roomRepo.Object);
        }

        // ---------- FindAvailableRoom ----------

        [Theory] // data-driven: invalid pairs should throw
        [MemberData(nameof(InvalidDatePairs))]
        public async Task FindAvailableRoom_InvalidDates_Throws((DateTime start, DateTime end) data)
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentException>(() => sut.FindAvailableRoom(data.start, data.end));
        }

        public static IEnumerable<object[]> InvalidDatePairs()
        {
            var today = DateTime.Today;
            yield return new object[] { (today, today.AddDays(1)) };            // start not in future
            yield return new object[] { (today, today) };                       // start not in future
            yield return new object[] { (today.AddDays(5), today.AddDays(4)) }; // start > end
        }

        [Fact]
        public async Task FindAvailableRoom_NoOverlaps_ReturnsARoomId()
        {
            var d = DateTime.Today.AddDays(10);
            var bookings = new[]
            {
                // Active booking in room 1 outside the day we want
                new Booking{ RoomId = 1, IsActive = true, StartDate = d.AddDays(5), EndDate = d.AddDays(6) }
            };

            var sut = CreateSut(bookings: bookings);
            var id = await sut.FindAvailableRoom(d, d);

            Assert.Contains(id, new[] { 1, 2, 3 });
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsOverlap_ReturnsMinusOne()
        {
            var d = DateTime.Today.AddDays(7);
            var rooms = new[] { new Room{Id=1}, new Room{Id=2}, new Room{Id=3} };
            var bookings = rooms.Select(r => new Booking
            {
                RoomId = r.Id, IsActive = true, StartDate = d, EndDate = d
            });

            var sut = CreateSut(rooms, bookings);
            var id = await sut.FindAvailableRoom(d, d);

            Assert.Equal(-1, id);
        }

        // ---------- CreateBooking ----------

        [Fact]
        public async Task CreateBooking_RoomFound_SetsRoomId_Activates_AndSaves()
        {
            var d = DateTime.Today.AddDays(14);
            var sut = CreateSut(); // default: rooms exist, no bookings

            var booking = new Booking { StartDate = d, EndDate = d.AddDays(1), CustomerId = 42 };

            var ok = await sut.CreateBooking(booking);

            Assert.True(ok);
            Assert.True(booking.IsActive);
            Assert.InRange(booking.RoomId, 1, 3);

            _bookingRepo.Verify(b => b.AddAsync(It.Is<Booking>(bk =>
                bk == booking && bk.IsActive && bk.RoomId > 0)), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_NoRoomFound_ReturnsFalse_AndDoesNotSave()
        {
            var d = DateTime.Today.AddDays(7);
            var rooms = new[] { new Room{Id=1}, new Room{Id=2}, new Room{Id=3} };
            var bookings = rooms.Select(r => new Booking {
                RoomId = r.Id, IsActive = true, StartDate = d, EndDate = d.AddDays(2)
            });

            var sut = CreateSut(rooms, bookings);
            var booking = new Booking { StartDate = d, EndDate = d.AddDays(1), CustomerId = 1 };

            var ok = await sut.CreateBooking(booking);

            Assert.False(ok);
            _bookingRepo.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // ---------- GetFullyOccupiedDates ----------

        [Fact]
        public async Task GetFullyOccupiedDates_ExactlyFullOnSomeDays_ReturnsThoseDates()
        {
            var rooms = new[] { new Room{Id=1}, new Room{Id=2} };
            var day1 = DateTime.Today.AddDays(30);
            var day2 = day1.AddDays(1);
            var day3 = day1.AddDays(2);

            // On day1 -> 2 overlapping active bookings (full),
            // day2 -> only 1 overlaps,
            // day3 -> only 1 overlaps.
            var bookings = new[]
            {
                new Booking{ RoomId=1, IsActive=true, StartDate=day1, EndDate=day2 }, // overlaps day1 & day2
                new Booking{ RoomId=2, IsActive=true, StartDate=day1, EndDate=day1 }, // overlaps day1
                new Booking{ RoomId=2, IsActive=true, StartDate=day3, EndDate=day3 }  // overlaps day3 only
            };

            var sut = CreateSut(rooms, bookings);

            var result = await sut.GetFullyOccupiedDates(day1, day3);

            Assert.Contains(day1, result);
            Assert.DoesNotContain(day2, result);
            Assert.DoesNotContain(day3, result);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_StartAfterEnd_Throws()
        {
            var sut = CreateSut();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.GetFullyOccupiedDates(DateTime.Today.AddDays(10), DateTime.Today.AddDays(9)));
        }
    }
}
