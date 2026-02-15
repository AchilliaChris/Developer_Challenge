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
    }
}
