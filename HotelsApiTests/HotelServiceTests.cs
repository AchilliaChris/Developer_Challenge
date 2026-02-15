using DataAccess;
using DeveloperChallenge;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelsApiTests
{
    public class HotelServiceTests
    {
        private HotelsDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelsDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new HotelsDbContext(options);
        }

        private HotelService CreateService(HotelsDbContext context)
        {
            ILogger<HotelService> logger = new LoggerFactory().CreateLogger<HotelService>();
            return new HotelService(context, new RoomBookingService(), logger);
        }

        [Fact]
        public async Task GetHotelByName_NullOrWhitespace_ThrowsArgumentException()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetHotelByName(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetHotelByName(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetHotelByName("  "));
        }

        [Fact]
        public async Task GetHotelByName_TooShort_ThrowsArgumentException()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.GetHotelByName("ab"));
            Assert.Contains("at least 3 characters", ex.Message);
        }

        [Fact]
        public async Task GetHotelByName_NotFound_ReturnsEmptyList()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            // seed a different hotel
            context.Hotels.Add(new Hotel { Name = "OtherHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetHotelByName("MissingHotel");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetHotelByName_Found_ReturnsHotelWithRooms_CaseInsensitive()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "MyHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, PricePerNight = 10.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            var resultLower = await service.GetHotelByName("myhotel");
            Assert.Single(resultLower);
            Assert.Single(resultLower.First().Rooms);

            var resultExact = await service.GetHotelByName("MyHotel");
            Assert.Single(resultExact);
            Assert.Single(resultExact.First().Rooms);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_NoHotels_ReturnsEmpty()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today, DateTime.Today.AddDays(1), 1);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_HotelWithNoBookings_ReturnsHotelAndRooms()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel {Name = "OpenHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var r1 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 10, Capacity = 2, PricePerNight = 30.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.Add(r1);
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 2);
            Assert.Single(result);
            Assert.Single(result.First().Rooms);
            Assert.Equal(2, result.First().Rooms.First().Capacity);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_RoomBookedDuringPeriod_Scenario1_Excluded()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "BusyHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 20, Capacity = 2, PricePerNight = 50.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing overlapping booking (2..4)
            var rb = new RoomBooking
            {
                Room_Id = room.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(4)
            };
            context.RoomBookings.Add(rb);
            context.SaveChanges();

            var service = CreateService(context);

            // Query for (1,3) overlaps existing scenario 1 -> should be excluded
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), 1);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_RoomBookedDuringPeriod_Scenario2_Excluded()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "BusyHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 20, Capacity = 2, PricePerNight = 50.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing overlapping booking (2..4)
            var rb = new RoomBooking
            {
                Room_Id = room.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(4)
            };
            context.RoomBookings.Add(rb);
            context.SaveChanges();

            var service = CreateService(context);

            // Query for (3..5) overlaps existing scenario 2 -> should be excluded
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(3), DateTime.Today.AddDays(5), 1);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_RoomBookedDuringPeriod_Scenario3_Excluded()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "BusyHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 20, Capacity = 2, PricePerNight = 50.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing overlapping booking (2.14)
            var rb = new RoomBooking
            {
                Room_Id = room.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(14)
            };
            context.RoomBookings.Add(rb);
            context.SaveChanges();

            var service = CreateService(context);

            // Query for (3..5) overlaps existing scenario 3 -> should be excluded
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(3), DateTime.Today.AddDays(5), 1);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_RoomBookedDuringPeriod_Scenario4_Excluded()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "BusyHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 20, Capacity = 2, PricePerNight = 50.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing overlapping booking (6,7)
            var rb = new RoomBooking
            {
                Room_Id = room.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(6),
                EndDate = DateTime.Today.AddDays(7)
            };
            context.RoomBookings.Add(rb);
            context.SaveChanges();

            var service = CreateService(context);

            // Query for (3..9) overlaps existing scenario 4 -> should be excluded
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(3), DateTime.Today.AddDays(9), 1);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_BookingEndsBeforeStart_IsNotOverlap_RoomAvailable()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "EdgeHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 30, Capacity = 2, PricePerNight = 60.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing booking that ends the day before requested start date begins
            var rb = new RoomBooking
            {
                Room_Id = room.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2) // checkout == requested start
            };
            context.RoomBookings.Add(rb);
            context.SaveChanges();

            var service = CreateService(context);

            // Request starts on EndDate => should be considered overlapping
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(3), DateTime.Today.AddDays(4), 1);
            Assert.Single(result);
            Assert.Single(result.First().Rooms);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_CapacitySumLessThanGuests_HotelExcluded()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "SmallHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            // Two rooms with capacity 1 each -> total 2
            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 1, PricePerNight = 10.0, RoomBookings = new List<RoomBooking>() });
            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 2, Capacity = 1, PricePerNight = 10.0, RoomBookings = new List<RoomBooking>() });
            context.SaveChanges();

            var service = CreateService(context);

            // Request for 3 guests: hotel should be removed because capacity < 3
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 3);
            Assert.Empty(result);

            // Request for 2 guests: should be included
            var result2 = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 2);
            Assert.Single(result2);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_PartiallyBooked_HotelReturnedWithFilteredRooms()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "PartialHotel", Phone = "+44 1234", Address = "12, High Street, Somewhere" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var availableRoom = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, Capacity = 2, PricePerNight = 40.0, RoomBookings = new List<RoomBooking>() };
            var bookedRoom = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 102, Capacity = 2, PricePerNight = 45.0, RoomBookings = new List<RoomBooking>() };
            context.Rooms.AddRange(availableRoom, bookedRoom);
            context.SaveChanges();

            // BookedRoom has overlapping booking
            var existing = new RoomBooking
            {
                Room_Id = bookedRoom.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(6)
            };
            context.RoomBookings.Add(existing);
            context.SaveChanges();

            var service = CreateService(context);

            // Request that overlaps the bookedRoom but not the availableRoom
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 1);
            Assert.Single(result);
            var returnedHotel = result.First();
            Assert.Single(returnedHotel.Rooms);
            Assert.Equal(availableRoom.RoomId, returnedHotel.Rooms.First().RoomId);

            // Request overlapping the booked room -> only availableRoom remains (as already tested)
            var result2 = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(4), DateTime.Today.AddDays(5), 1);
            Assert.Single(result2);
            Assert.Single(result2.First().Rooms);
            Assert.Equal(availableRoom.RoomId, result2.First().Rooms.First().RoomId);
        }

        [Fact]
        public async Task GetHotelByName_MultipleHotels_ReturnsOnlyMatching()
        {
            // Test that only matching hotels are returned when multiple exist
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            context.Hotels.Add(new Hotel { Name = "Hotel A", Phone = "+44 111", Address = "Address A" });
            context.Hotels.Add(new Hotel { Name = "Hotel B", Phone = "+44 222", Address = "Address B" });
            context.Hotels.Add(new Hotel { Name = "Hotel C", Phone = "+44 333", Address = "Address C" });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetHotelByName("Hotel B");
            Assert.Single(result);
            Assert.Equal("Hotel B", result.First().Name);
        }

        [Fact]
        public async Task GetHotelByName_PartialMatch_ReturnsMatching()
        {
            // Test that partial name matching doesn't work (exact match required)
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            context.Hotels.Add(new Hotel { Name = "Grand Hotel Downtown", Phone = "+44 111", Address = "Address" });
            context.Hotels.Add(new Hotel { Name = "Grand Hotel Uptown", Phone = "+44 222", Address = "Address" });
            context.SaveChanges();

            var service = CreateService(context);

            // Partial match doesn't work - requires minimum 3 chars and exact match
            var result = await service.GetHotelByName("Grand");
            Assert.Empty(result); // No partial matching
        }

        [Fact]
        public async Task GetHotelByName_ExactMatchAmongMany_ReturnsOnly()
        {
            // Test exact matching among similar names
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            context.Hotels.Add(new Hotel { Name = "Grand Hotel Downtown", Phone = "+44 111", Address = "Address" });
            context.Hotels.Add(new Hotel { Name = "Grand Hotel Uptown", Phone = "+44 222", Address = "Address" });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetHotelByName("Grand Hotel Downtown");
            Assert.Single(result);
            Assert.Equal("Grand Hotel Downtown", result.First().Name);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_DatesMustBeOrdered_NotAllowed()
        {
            // Test that service validates startDate < endDate
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Test Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 50.0 });
            context.SaveChanges();

            var service = CreateService(context);

            // StartDate must be before EndDate
            var singleDay = DateTime.Today.AddDays(5);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetAvailableHotelRooms(singleDay, singleDay, 1));
        }

        [Fact]
        public async Task GetAvailableHotelRooms_MultipleHotels_AllIncluded()
        {
            // Test that multiple available hotels are returned
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel1 = new Hotel { Name = "Hotel 1", Phone = "+44 111", Address = "Address 1" };
            var hotel2 = new Hotel { Name = "Hotel 2", Phone = "+44 222", Address = "Address 2" };
            var hotel3 = new Hotel { Name = "Hotel 3", Phone = "+44 333", Address = "Address 3" };
            context.Hotels.AddRange(hotel1, hotel2, hotel3);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel1.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 50.0 });
            context.Rooms.Add(new Room { Hotel_Id = hotel2.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 60.0 });
            context.Rooms.Add(new Room { Hotel_Id = hotel3.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 70.0 });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 1);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_ManyRooms_AllIncluded()
        {
            // Test hotel with many rooms
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Big Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            // Add 20 rooms
            for (int i = 1; i <= 20; i++)
            {
                context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = i, Capacity = 2, PricePerNight = 50.0 });
            }
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 1);
            Assert.Single(result);
            Assert.Equal(20, result.First().Rooms.Count);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_ExactCapacityMatch_Included()
        {
            // Test hotel with exact guest capacity match
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Exact Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 5, PricePerNight = 50.0 });
            context.SaveChanges();

            var service = CreateService(context);

            // Request exactly 5 guests
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 5);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_CapacityExceedsGuests_Included()
        {
            // Test hotel with excess capacity included
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Spacious Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 10, PricePerNight = 50.0 });
            context.SaveChanges();

            var service = CreateService(context);

            // Request 3 guests, hotel has capacity for 10
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 3);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_VariousRoomPrices_AllIncluded()
        {
            // Test hotel with rooms at different price points
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Varied Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, Capacity = 1, PricePerNight = 30.0 });
            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 102, Capacity = 2, PricePerNight = 50.0 });
            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 103, Capacity = 4, PricePerNight = 100.0 });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 1);
            Assert.Single(result);
            Assert.Equal(3, result.First().Rooms.Count);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_LongStay_Available()
        {
            // Test availability for long stay (30+ days)
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Long Stay Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 50.0 });
            context.SaveChanges();

            var service = CreateService(context);

            // Request 30 days
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(31);
            var result = await service.GetAvailableHotelRooms(startDate, endDate, 1);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_PartialBookings_OnlyAvailableRoomsReturned()
        {
            // Test when hotel has some booked and some available rooms
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Partially Booked", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room1 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 50.0 };
            var room2 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 2, Capacity = 2, PricePerNight = 50.0 };
            var room3 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 3, Capacity = 2, PricePerNight = 50.0 };
            context.Rooms.AddRange(room1, room2, room3);
            context.SaveChanges();

            // Book room 2
            var roomBooking = new RoomBooking
            {
                Room_Id = room2.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(3)
            };
            context.RoomBookings.Add(roomBooking);
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 1);
            Assert.Single(result);
            Assert.Equal(2, result.First().Rooms.Count); // Only rooms 1 and 3
        }

        [Fact]
        public async Task GetAvailableHotelRooms_MultipleBookings_CorrectlyFiltered()
        {
            // Test room with multiple bookings, only overlapping filtered out
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Busy Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 50.0 };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Add multiple bookings
            context.RoomBookings.Add(new RoomBooking { Room_Id = room.RoomId, Booking_Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2) });
            context.RoomBookings.Add(new RoomBooking { Room_Id = room.RoomId, Booking_Id = 2, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(6) });
            context.RoomBookings.Add(new RoomBooking { Room_Id = room.RoomId, Booking_Id = 3, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(11) });
            context.SaveChanges();

            var service = CreateService(context);

            // Query for available period between bookings
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(3), DateTime.Today.AddDays(4), 1);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_HighCapacityRooms_Available()
        {
            // Test hotel with high capacity rooms
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Group Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            context.Rooms.Add(new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 20, PricePerNight = 200.0 });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 15);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_NoAvailableHotels_ReturnsEmpty()
        {
            // Test when all hotels are fully booked
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "Full Hotel", Phone = "+44 111", Address = "Address" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 1, Capacity = 2, PricePerNight = 50.0 };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Book the only room
            var roomBooking = new RoomBooking
            {
                Room_Id = room.RoomId,
                Booking_Id = 1,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(10)
            };
            context.RoomBookings.Add(roomBooking);
            context.SaveChanges();

            var service = CreateService(context);

            // Request overlapping the booking
            var result = await service.GetAvailableHotelRooms(DateTime.Today.AddDays(2), DateTime.Today.AddDays(5), 1);
            Assert.Empty(result);
        }
    }
}
