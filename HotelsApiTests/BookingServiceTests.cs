using DataAccess;
using DeveloperChallenge;
using DeveloperChallenge.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelsApiTests
{
    public class BookingServiceTests
    {
        private HotelsDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelsDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new HotelsDbContext(options);
        }

        private BookingService CreateService(HotelsDbContext context)
        {
            var sqidsOptions = new Sqids.SqidsOptions { Alphabet = "F4ZL2T1pa7vROSAwX6cMhoJ0KtIjDHu8ikY9VfCBrbzyesl5GU3WqdxmPNEQgn" };
            var sqids = new Sqids.SqidsEncoder<int>(sqidsOptions);
            var logger = NullLogger<BookingService>.Instance;
            var roomBookingService = new RoomBookingService();
            return new BookingService(sqids, context, logger, roomBookingService);
        }

        [Fact]
        public async Task CreateBooking_Success_CreatesBookingAndRoomBookings()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            // Seed hotel and room
            var hotel = new Hotel { Name = "TestHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();
            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, PricePerNight = 50.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            // Build booking request (2 nights: Start and End inclusive)
            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 101,
                        PricePerNight = 50.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone="555" }
                        }
                    }
                },
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var response = await service.CreateBooking(request);

            Assert.NotNull(response);
            Assert.NotNull(response.bookingResponse.BookingReference);
            // Number of days = (End - Start).Days +1 = 2. They stay on the first and second nights and check out the third morning.
            Assert.Equal(50.0 * 2, response.bookingResponse.TotalPrice);
            Assert.Single(response.bookingResponse.RoomBookings);

            // Verify DB state
            var bookingInDb = context.Bookings.Include(b => b.RoomBookings).FirstOrDefault();
            Assert.NotNull(bookingInDb);
            Assert.Single(context.RoomBookings.Where(rb => rb.Booking_Id == bookingInDb.BookingId));
            Assert.Single(context.Customers.Where(c => c.Email == "john@example.com"));
            Assert.Single(context.GuestBookings.Where(g => g.RoomBooking_Id == context.RoomBookings.First().Room_Id));
        }

        [Fact]
        public async Task CreateBooking_HotelNotFound_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "A", LastName = "B", Email = "a@b.com", Phone = "1" },
                Hotel = new HotelBookingViewModel { Name = "MissingHotel" },
                Rooms = new List<RoomBookingViewModel>(),
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(1)
            };

            var result = await service.CreateBooking(request);
            Assert.Contains("Hotel not found", result.message);
        }

        [Fact]
        public async Task GetBookingByReference_Success_ReturnsCorrectMapping()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            // Seed customer, hotel, room, booking, roomBooking, guest, guestBooking
            var customer = new Customer { FirstName = "Alice", LastName = "Smith", Email = "alice@test", Phone = "9" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var hotel = new Hotel { Name = "MapHotel", Address = "X", Phone = "0" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 200, PricePerNight = 80.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "MAP-REF", TotalPrice = 160.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var roomBooking = new RoomBooking { Booking_Id = booking.BookingId, Room_Id = room.RoomId, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2) };
            context.RoomBookings.Add(roomBooking);
            context.SaveChanges();

            var guest = new Customer { FirstName = "Guest", LastName = "One", Email = "guest1", Phone = "0" };
            context.Customers.Add(guest);
            context.SaveChanges();

            var guestBooking = new GuestBooking { RoomBooking_Id = roomBooking.RoomBookingId, GuestId = guest.CustomerId };
            context.GuestBookings.Add(guestBooking);
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetBookingByReference("MAP-REF");

            Assert.NotNull(result);
            Assert.Equal("Alice Smith", result.CustomerName);
            Assert.Equal(160.0, result.TotalPrice);
            Assert.Single(result.RoomBookings);
            var rb = result.RoomBookings.First();
            Assert.Equal(hotel.Name, rb.HotelName);
            Assert.Equal(room.RoomNumber.ToString(), rb.RoomNumber);
            Assert.Single(rb.Guests);
            Assert.Contains("Guest One", rb.Guests.First());
        }

        [Fact]
        public async Task GetBookingByReference_NotFound_ThrowsException()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByReference("DOESNOTEXIST"));
            Assert.Contains("Booking not found", ex.Message);
        }

        [Fact]
        public async Task CreateBooking_ExistingCustomer_ReusesCustomer()
        {
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            // Seed existing customer with same email
            var existing = new Customer { FirstName = "Existing", LastName = "User", Email = "dup@example", Phone = "1" };
            context.Customers.Add(existing);
            context.SaveChanges();

            var hotel = new Hotel { Name = "DupHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 10, PricePerNight = 20.0, Capacity = 1 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "NewFirst", LastName = "NewLast", Email = "dup@example", Phone = "1" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 10,
                        PricePerNight = 20.0,
                        Capacity = 1,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "NewFirst", LastName = "NewLast", Email = "dup@example", Phone="1" }
                        }
                    }
                },
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(3)
            };

            var beforeCount = context.Customers.Count();
            var resp = await service.CreateBooking(request);
            var afterCount = context.Customers.Count();

            // No new customer should be created for same email
            Assert.Equal(beforeCount, afterCount);
            Assert.Equal($"NewFirst NewLast", resp.bookingResponse.CustomerName);
        }

        [Fact]
        public async Task CreateBooking_OverlappingBookings_DoesntAllowScenario1OverlappingRoomBookings()
        {
            // This test covers an attempt to make an overlapping booking for a room that is already booked.
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "OverlapHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 55, PricePerNight = 30.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing booking
            var customer = new Customer { FirstName = "X", LastName = "Y", Email = "xy", Phone = "0" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "OLD", TotalPrice = 60.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var existingRoomBooking = new RoomBooking
            {
                Booking_Id = booking.BookingId,
                Room_Id = room.RoomId,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(4)
            };
            context.RoomBookings.Add(existingRoomBooking);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "New", LastName = "Person", Email = "new@p", Phone = "0" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 55,
                        PricePerNight = 30.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "New", LastName = "Person", Email = "new@p", Phone="0" }
                        }
                    }
                },
                // Overlaps existingRoomBooking (2..4) with (3..5)
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(5)
            };

            var resp = await service.CreateBooking(request);

            // Code rejects an additional RoomBooking which overlaps
            Assert.Null(resp.bookingResponse.BookingReference);
            var createdRoomBookings = context.RoomBookings.Count(rb => rb.Room_Id == room.RoomId);
            Assert.Equal(1, createdRoomBookings); // old only
            Assert.Contains("Room not available", resp.message);
        }

        [Fact]
        public async Task CreateBooking_OverlappingBookings_DoesntAllowScenario2OverlappingRoomBookings()
        {
            // This test covers an attempt to make an overlapping booking for a room that is already booked.
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "OverlapHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 55, PricePerNight = 30.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing booking
            var customer = new Customer { FirstName = "X", LastName = "Y", Email = "xy", Phone = "0" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "OLD", TotalPrice = 60.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var existingRoomBooking = new RoomBooking
            {
                Booking_Id = booking.BookingId,
                Room_Id = room.RoomId,
                StartDate = DateTime.Today.AddDays(4),
                EndDate = DateTime.Today.AddDays(6)
            };
            context.RoomBookings.Add(existingRoomBooking);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "New", LastName = "Person", Email = "new@p", Phone = "0" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 55,
                        PricePerNight = 30.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "New", LastName = "Person", Email = "new@p", Phone="0" }
                        }
                    }
                },
                // Overlaps existingRoomBooking (4..6) with (3..5)
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(5)
            };

            var resp = await service.CreateBooking(request);

            // Code rejects an additional RoomBooking which overlaps
            Assert.Null(resp.bookingResponse.BookingReference);
            var createdRoomBookings = context.RoomBookings.Count(rb => rb.Room_Id == room.RoomId);
            Assert.Equal(1, createdRoomBookings); // old only
            Assert.Contains("Room not available", resp.message);
        }

        [Fact]
        public async Task CreateBooking_OverlappingBookings_DoesntAllowScenario3OverlappingRoomBookings()
        {
            // This test covers an attempt to make an overlapping booking for a room that is already booked.
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "OverlapHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 55, PricePerNight = 30.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing booking
            var customer = new Customer { FirstName = "X", LastName = "Y", Email = "xy", Phone = "0" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "OLD", TotalPrice = 60.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var existingRoomBooking = new RoomBooking
            {
                Booking_Id = booking.BookingId,
                Room_Id = room.RoomId,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(8)
            };
            context.RoomBookings.Add(existingRoomBooking);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "New", LastName = "Person", Email = "new@p", Phone = "0" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 55,
                        PricePerNight = 30.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "New", LastName = "Person", Email = "new@p", Phone="0" }
                        }
                    }
                },
                // Overlaps existingRoomBooking (2..8) with (3..5)
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(5)
            };

            var resp = await service.CreateBooking(request);

            // Code rejects an additional RoomBooking which overlaps
            Assert.Null(resp.bookingResponse.BookingReference);
            var createdRoomBookings = context.RoomBookings.Count(rb => rb.Room_Id == room.RoomId);
            Assert.Equal(1, createdRoomBookings); // old only
            Assert.Contains("Room not available", resp.message);
        }

        [Fact]
        public async Task CreateBooking_OverlappingBookings_DoesntAllowScenario4OverlappingRoomBookings()
        {
            // This test covers an attempt to make an overlapping booking for a room that is already booked.
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "OverlapHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 55, PricePerNight = 30.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            // Existing booking
            var customer = new Customer { FirstName = "X", LastName = "Y", Email = "xy", Phone = "0" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "OLD", TotalPrice = 60.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var existingRoomBooking = new RoomBooking
            {
                Booking_Id = booking.BookingId,
                Room_Id = room.RoomId,
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(5)
            };
            context.RoomBookings.Add(existingRoomBooking);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "New", LastName = "Person", Email = "new@p", Phone = "0" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 55,
                        PricePerNight = 30.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "New", LastName = "Person", Email = "new@p", Phone="0" }
                        }
                    }
                },
                // Overlaps existingRoomBooking (3..5) with (2..8)
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(8)
            };

            var resp = await service.CreateBooking(request);

            // Code rejects an additional RoomBooking which overlaps
            Assert.Null(resp.bookingResponse.BookingReference);
            var createdRoomBookings = context.RoomBookings.Count(rb => rb.Room_Id == room.RoomId);
            Assert.Equal(1, createdRoomBookings); // old only
            Assert.Contains("Room not available", resp.message);
        }


    }
}