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

        // ==========================================================
        // FindAvailableRoom
        // ==========================================================

        [Theory]
        [MemberData(nameof(InvalidDatePairs))]
        public async Task FindAvailableRoom_InvalidDates_Throws((DateTime start, DateTime end) data)
        {
            // Arrange
            var sut = CreateSut();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => sut.FindAvailableRoom(data.start, data.end));
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsOverlap_ReturnsMinusOne()
        {
            // Arrange
            var d = DateTime.Today.AddDays(7);
            var rooms = new[] { new Room { Id = 1 }, new Room { Id = 2 }, new Room { Id = 3 } };
            var bookings = rooms.Select(r => new Booking
            {
                RoomId = r.Id, IsActive = true, StartDate = d, EndDate = d
            });
            var sut = CreateSut(rooms, bookings);

            // Act
            var id = await sut.FindAvailableRoom(d, d);

            // Assert
            Assert.Equal(-1, id);
        }

        [Fact]
        public async Task FindAvailableRoom_NoOverlaps_ReturnsARoomId()
        {
            // Arrange
            var d = DateTime.Today.AddDays(10);
            var bookings = new[]
            {
                new Booking{ RoomId = 1, IsActive = true, StartDate = d.AddDays(5), EndDate = d.AddDays(6) }
            };
            var sut = CreateSut(bookings: bookings);

            // Act
            var id = await sut.FindAvailableRoom(d, d);

            // Assert
            Assert.InRange(id, 1, 3);
        }

        [Fact]
        public async Task FindAvailableRoom_OnlyInactiveBookingsExist_ReturnsFirstRoom()
        {
            // Arrange
            var date = DateTime.Today.AddDays(7);
            var rooms = new[] { new Room { Id = 1 }, new Room { Id = 2 }, new Room { Id = 3 } };
            var bookings = rooms.Select(r => new Booking
            {
                RoomId = r.Id, IsActive = false, StartDate = date, EndDate = date
            }).ToArray();
            var sut = CreateSut(rooms, bookings);

            // Act
            var result = await sut.FindAvailableRoom(date, date);

            // Assert
            Assert.Equal(1, result);
        }

        public static IEnumerable<object[]> InvalidDatePairs()
        {
            var today = DateTime.Today;
            yield return new object[] { (today, today.AddDays(1)) };
            yield return new object[] { (today, today) };
            yield return new object[] { (today.AddDays(5), today.AddDays(4)) };
        }

        // ==========================================================
        // CreateBooking
        // ==========================================================

        [Fact]
        public async Task CreateBooking_RoomFound_UpdatesBookingAndSaves()
        {
            // Arrange
            var d = DateTime.Today.AddDays(14);
            var sut = CreateSut();
            var booking = new Booking { StartDate = d, EndDate = d.AddDays(1), CustomerId = 42, RoomId = 999 };

            // Act
            var ok = await sut.CreateBooking(booking);

            // Assert
            Assert.True(ok);
            Assert.True(booking.IsActive);
            Assert.InRange(booking.RoomId, 1, 3);
            Assert.Equal(42, booking.CustomerId);
            _bookingRepo.Verify(b => b.AddAsync(It.Is<Booking>(bk =>
                bk == booking && bk.IsActive && bk.RoomId > 0)), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_NoRoomFound_ReturnsFalse_AndDoesNotSave()
        {
            // Arrange
            var d = DateTime.Today.AddDays(7);
            var rooms = new[] { new Room { Id = 1 }, new Room { Id = 2 }, new Room { Id = 3 } };
            var bookings = rooms.Select(r => new Booking {
                RoomId = r.Id, IsActive = true, StartDate = d, EndDate = d.AddDays(2)
            });
            var sut = CreateSut(rooms, bookings);
            var booking = new Booking { StartDate = d, EndDate = d.AddDays(1), CustomerId = 1 };

            // Act
            var ok = await sut.CreateBooking(booking);

            // Assert
            Assert.False(ok);
            _bookingRepo.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // ==========================================================
        // GetFullyOccupiedDates
        // ==========================================================

        [Fact]
        public async Task GetFullyOccupiedDates_ExactlyFullOnSomeDays_ReturnsThoseDates()
        {
            // Arrange
            var rooms = new[] { new Room { Id = 1 }, new Room { Id = 2 } };
            var day1 = DateTime.Today.AddDays(30);
            var day2 = day1.AddDays(1);
            var day3 = day1.AddDays(2);
            var bookings = new[]
            {
                new Booking{ RoomId=1, IsActive=true, StartDate=day1, EndDate=day2 },
                new Booking{ RoomId=2, IsActive=true, StartDate=day1, EndDate=day1 },
                new Booking{ RoomId=2, IsActive=true, StartDate=day3, EndDate=day3 }
            };
            var sut = CreateSut(rooms, bookings);

            // Act
            var result = await sut.GetFullyOccupiedDates(day1, day3);

            // Assert
            Assert.Contains(day1, result);
            Assert.DoesNotContain(day2, result);
            Assert.DoesNotContain(day3, result);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_StartAfterEnd_Throws()
        {
            // Arrange
            var sut = CreateSut();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.GetFullyOccupiedDates(DateTime.Today.AddDays(10), DateTime.Today.AddDays(9)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(30)]
        public async Task GetFullyOccupiedDates_NoDaysFullyOccupied_ReturnsEmptyList(int daysAhead)
        {
            // Arrange
            var rooms = new[] { new Room { Id = 1 }, new Room { Id = 2 }, new Room { Id = 3 } };
            var startDate = DateTime.Today.AddDays(daysAhead);
            var endDate = startDate.AddDays(7);
            var bookings = new[]
            {
                new Booking{ RoomId=1, IsActive=true, StartDate=startDate, EndDate=startDate.AddDays(1) },
                new Booking{ RoomId=2, IsActive=true, StartDate=startDate.AddDays(2), EndDate=startDate.AddDays(3) }
            };
            var sut = CreateSut(rooms, bookings);

            // Act
            var result = await sut.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [MemberData(nameof(InactiveBookingData))]
        public async Task GetFullyOccupiedDates_InactiveBookingsIgnored_ReturnsCorrectDates(
            IEnumerable<Booking> bookings, DateTime startDate, DateTime endDate, int expectedFullDays)
        {
            // Arrange
            var rooms = new[] { new Room { Id = 1 }, new Room { Id = 2 } };
            var sut = CreateSut(rooms, bookings);

            // Act
            var result = await sut.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(expectedFullDays, result.Count);
        }

        public static IEnumerable<object[]> InactiveBookingData()
        {
            var baseDate = DateTime.Today.AddDays(15);

            yield return new object[]
            {
                new[]
                {
                    new Booking{ RoomId=1, IsActive=false, StartDate=baseDate, EndDate=baseDate },
                    new Booking{ RoomId=2, IsActive=false, StartDate=baseDate, EndDate=baseDate }
                },
                baseDate, baseDate, 0
            };

            yield return new object[]
            {
                new[]
                {
                    new Booking{ RoomId=1, IsActive=true, StartDate=baseDate, EndDate=baseDate },
                    new Booking{ RoomId=2, IsActive=false, StartDate=baseDate, EndDate=baseDate }
                },
                baseDate, baseDate, 0
            };
        }
    }
}
