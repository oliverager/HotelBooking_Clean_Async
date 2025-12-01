using HotelBooking.Core;
using Xunit;

namespace HotelBooking.DD_PathTests
{
    // Simple in-memory fake repository used for DD-path tests
    internal class FakeRepository<T> : IRepository<T>
    {
        private readonly List<T> data;

        public FakeRepository(IEnumerable<T> initialData = null)
        {
            data = initialData?.ToList() ?? new List<T>();
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<T>>(data);
        }

        // The remaining members are not used in these tests
        public Task<T> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task EditAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(int id)
        {
            throw new NotImplementedException();
        }
    }

    public class BookingManagerDdPathTests
    {
        // ---------- P1–P4 : FindAvailableRoom ----------

        // P1 – invalid dates -> exception (early exit)
        [Fact]
        public async Task FindAvailableRoom_P1_InvalidDates_ThrowsArgumentException()
        {
            // Arrange
            var bookingRepo = new FakeRepository<Booking>();
            var roomRepo = new FakeRepository<Room>();
            var manager = new BookingManager(bookingRepo, roomRepo);

            var startDate = DateTime.Today; // not in the future
            var endDate = DateTime.Today.AddDays(1);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                manager.FindAvailableRoom(startDate, endDate));
        }

        // P2 – valid dates, at least one available room -> returns room id
        [Fact]
        public async Task FindAvailableRoom_P2_RoomAvailable_ReturnsRoomId()
        {
            // Arrange
            var today = DateTime.Today;

            var rooms = new[]
            {
                new Room { Id = 1, Description = "Room 1" }
            };

            var bookings = new[]
            {
                // Booking for the same room but in the future, non-overlapping
                new Booking
                {
                    Id = 1,
                    RoomId = 1,
                    StartDate = today.AddDays(3),
                    EndDate = today.AddDays(5),
                    IsActive = true
                }
            };

            var bookingRepo = new FakeRepository<Booking>(bookings);
            var roomRepo = new FakeRepository<Room>(rooms);
            var manager = new BookingManager(bookingRepo, roomRepo);

            var startDate = today.AddDays(1);
            var endDate = today.AddDays(1);

            // Act
            var roomId = await manager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, roomId);
        }

        // P3 – valid dates, no room available -> returns -1
        [Fact]
        public async Task FindAvailableRoom_P3_NoRoomAvailable_ReturnsMinusOne()
        {
            // Arrange
            var today = DateTime.Today;

            var rooms = new[]
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            var startDate = today.AddDays(1);
            var endDate = startDate;

            var bookings = new[]
            {
                // Both rooms have active bookings overlapping the requested day
                new Booking
                {
                    Id = 1,
                    RoomId = 1,
                    StartDate = today, // [today, today+2]
                    EndDate = today.AddDays(2),
                    IsActive = true
                },
                new Booking
                {
                    Id = 2,
                    RoomId = 2,
                    StartDate = today, // [today, today+3]
                    EndDate = today.AddDays(3),
                    IsActive = true
                }
            };

            var bookingRepo = new FakeRepository<Booking>(bookings);
            var roomRepo = new FakeRepository<Room>(rooms);
            var manager = new BookingManager(bookingRepo, roomRepo);

            // Act
            var roomId = await manager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, roomId);
        }

        // P4 – room with no bookings (All on empty set) -> returns that room
        [Fact]
        public async Task FindAvailableRoom_P4_RoomWithNoBookings_IsSelected()
        {
            // Arrange
            var today = DateTime.Today;

            var rooms = new[]
            {
                new Room { Id = 1, Description = "Booked room" },
                new Room { Id = 2, Description = "Completely free room" }
            };

            var startDate = today.AddDays(1);
            var endDate = startDate;

            var bookings = new[]
            {
                // Room 1 has an overlapping booking
                new Booking
                {
                    Id = 1,
                    RoomId = 1,
                    StartDate = today, // covers startDate
                    EndDate = today.AddDays(2),
                    IsActive = true
                }
                // Room 2 has no bookings at all
            };

            var bookingRepo = new FakeRepository<Booking>(bookings);
            var roomRepo = new FakeRepository<Room>(rooms);
            var manager = new BookingManager(bookingRepo, roomRepo);

            // Act
            var roomId = await manager.FindAvailableRoom(startDate, endDate);

            // Assert: the room without bookings should be selected
            Assert.Equal(2, roomId);
        }

        // ---------- Q1–Q4 : GetFullyOccupiedDates ----------

        // Q1 – invalid range (start > end) -> exception
        [Fact]
        public async Task GetFullyOccupiedDates_Q1_InvalidRange_ThrowsArgumentException()
        {
            // Arrange
            var bookingRepo = new FakeRepository<Booking>();
            var roomRepo = new FakeRepository<Room>();
            var manager = new BookingManager(bookingRepo, roomRepo);

            var startDate = new DateTime(2025, 1, 10);
            var endDate = new DateTime(2025, 1, 5);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                manager.GetFullyOccupiedDates(startDate, endDate));
        }

        // Q2 – valid range, no bookings at all -> empty result
        [Fact]
        public async Task GetFullyOccupiedDates_Q2_NoBookings_ReturnsEmptyList()
        {
            // Arrange
            var rooms = new[]
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" },
                new Room { Id = 3, Description = "Room 3" }
            };

            var bookingRepo = new FakeRepository<Booking>(Enumerable.Empty<Booking>());
            var roomRepo = new FakeRepository<Room>(rooms);
            var manager = new BookingManager(bookingRepo, roomRepo);

            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 3);

            // Act
            var dates = await manager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(dates);
        }

        
        // Q3 – bookings exist but no day is fully occupied -> empty result
        [Fact]
        public async Task GetFullyOccupiedDates_Q3_NoDayFullyOccupied_ReturnsEmptyList()
        {
            // Arrange
            var rooms = new[]
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" },
                new Room { Id = 3, Description = "Room 3" }
            };

            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 3);

            var bookings = new[]
            {
                // Day 1: only room 1
                new Booking
                {
                    Id = 1,
                    RoomId = 1,
                    StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 1, 3),
                    IsActive = true
                },
                // Day 2 & 3: rooms 1 and 2 → at most 2 bookings per day (never 3)
                new Booking
                {
                    Id = 2,
                    RoomId = 2,
                    StartDate = new DateTime(2025, 1, 2),
                    EndDate = new DateTime(2025, 1, 3),
                    IsActive = true
                }
                // Room 3 has no bookings
            };

            var bookingRepo = new FakeRepository<Booking>(bookings);
            var roomRepo = new FakeRepository<Room>(rooms);
            var manager = new BookingManager(bookingRepo, roomRepo);

            // Act
            var dates = await manager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(dates);
        }

        // Q4 – at least one fully occupied date
        [Fact]
        public async Task GetFullyOccupiedDates_Q4_SomeDaysFullyOccupied_ReturnsThoseDays()
        {
            // Arrange
            var rooms = new[]
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" },
                new Room { Id = 3, Description = "Room 3" }
            };

            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 3);

            var fullyOccupiedDay = new DateTime(2025, 1, 2);

            var bookings = new[]
            {
                // All three rooms booked on 2 Jan (fully occupied day)
                new Booking
                {
                    Id = 1,
                    RoomId = 1,
                    StartDate = fullyOccupiedDay,
                    EndDate = fullyOccupiedDay,
                    IsActive = true
                },
                new Booking
                {
                    Id = 2,
                    RoomId = 2,
                    StartDate = fullyOccupiedDay,
                    EndDate = fullyOccupiedDay,
                    IsActive = true
                },
                new Booking
                {
                    Id = 3,
                    RoomId = 3,
                    StartDate = fullyOccupiedDay,
                    EndDate = fullyOccupiedDay,
                    IsActive = true
                },
                // Extra booking so that another day is NOT fully occupied
                new Booking
                {
                    Id = 4,
                    RoomId = 1,
                    StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 1, 1),
                    IsActive = true
                }
            };

            var bookingRepo = new FakeRepository<Booking>(bookings);
            var roomRepo = new FakeRepository<Room>(rooms);
            var manager = new BookingManager(bookingRepo, roomRepo);

            // Act
            var dates = await manager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Contains(fullyOccupiedDay, dates);
            Assert.DoesNotContain(new DateTime(2025, 1, 1), dates);
            Assert.DoesNotContain(new DateTime(2025, 1, 3), dates);
        }
    }
}