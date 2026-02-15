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

        [Fact]
        public async Task CreateBooking_MultipleRooms_Success()
        {
            // Test creating a booking with multiple room bookings
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            // Seed hotel and multiple rooms
            var hotel = new Hotel { Name = "MultiRoomHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room1 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, PricePerNight = 50.0, Capacity = 2 };
            var room2 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 102, PricePerNight = 75.0, Capacity = 3 };
            context.Rooms.Add(room1);
            context.Rooms.Add(room2);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Multi", LastName = "Booker", Email = "multi@test.com", Phone = "555" },
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
                            new CustomerViewModel{ FirstName = "Guest", LastName = "One", Email = "guest1@test.com", Phone="555" }
                        }
                    },
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 102,
                        PricePerNight = 75.0,
                        Capacity = 3,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Guest", LastName = "Two", Email = "guest2@test.com", Phone="555" }
                        }
                    }
                },
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var response = await service.CreateBooking(request);

            Assert.NotNull(response);
            Assert.NotNull(response.bookingResponse.BookingReference);
            // 2 rooms * (50.0 + 75.0) = 250.0 for 2 nights
            Assert.Equal(250.0, response.bookingResponse.TotalPrice);
            Assert.Equal(2, response.bookingResponse.RoomBookings.Count);
        }

        [Fact]
        public async Task CreateBooking_SingleNightStay_CalculatesCorrectPrice()
        {
            // Test single night stay (StartDate == EndDate)
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "SingleNightHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, PricePerNight = 100.0, Capacity = 1 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            var singleDate = DateTime.Today.AddDays(5);
            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Single", LastName = "Night", Email = "single@night.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 101,
                        PricePerNight = 100.0,
                        Capacity = 1,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Single", LastName = "Night", Email = "single@night.com", Phone="555" }
                        }
                    }
                },
                StartDate = singleDate,
                EndDate = singleDate
            };

            var response = await service.CreateBooking(request);

            Assert.NotNull(response);
            Assert.NotNull(response.bookingResponse.BookingReference);
            // Single night = 1 day
            Assert.Equal(100.0, response.bookingResponse.TotalPrice);
        }

        [Fact]
        public async Task CreateBooking_WithMultipleGuests_AllGuestsRecorded()
        {
            // Test booking with multiple guests in one room
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "GroupHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, PricePerNight = 50.0, Capacity = 4 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Group", LastName = "Leader", Email = "leader@group.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 101,
                        PricePerNight = 50.0,
                        Capacity = 4,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Guest", LastName = "One", Email = "g1@group.com", Phone="555" },
                            new CustomerViewModel{ FirstName = "Guest", LastName = "Two", Email = "g2@group.com", Phone="555" },
                            new CustomerViewModel{ FirstName = "Guest", LastName = "Three", Email = "g3@group.com", Phone="555" },
                            new CustomerViewModel{ FirstName = "Guest", LastName = "Four", Email = "g4@group.com", Phone="555" }
                        }
                    }
                },
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(3)
            };

            var response = await service.CreateBooking(request);

            Assert.NotNull(response);
            Assert.NotNull(response.bookingResponse.BookingReference);
            // Price: 50 * 3 nights = 150
            Assert.Equal(150.0, response.bookingResponse.TotalPrice);
            Assert.Single(response.bookingResponse.RoomBookings);
            Assert.Equal(4, response.bookingResponse.RoomBookings.First().Guests.Count);
        }

        [Fact]
        public async Task CreateBooking_RoomNotFound_ReturnsError()
        {
            // Test creating booking for room that doesn't exist
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "TestHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 999, // Non-existent room
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

            Assert.Null(response.bookingResponse.BookingReference);
            Assert.Contains("Room not found", response.message);
        }

        [Fact]
        public async Task CreateBooking_NoRooms_CreatesBookingWithoutRooms()
        {
            // Test creating booking with empty rooms list - should still create booking
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "TestHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>(), // Empty rooms
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var response = await service.CreateBooking(request);

            // Based on actual behavior, booking is still created with reference but zero price
            Assert.NotNull(response.bookingResponse.BookingReference);
            Assert.Equal(0.0, response.bookingResponse.TotalPrice);
            Assert.Empty(response.bookingResponse.RoomBookings);
        }

        [Fact]
        public async Task GetBookingByReference_WithMultipleRoomBookings_ReturnsAll()
        {
            // Test GetBookingByReference with booking containing multiple rooms
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var customer = new Customer { FirstName = "Alice", LastName = "Smith", Email = "alice@test", Phone = "9" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var hotel = new Hotel { Name = "MultiRoomHotel", Address = "X", Phone = "0" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room1 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 200, PricePerNight = 80.0, Capacity = 2 };
            var room2 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 201, PricePerNight = 100.0, Capacity = 3 };
            context.Rooms.Add(room1);
            context.Rooms.Add(room2);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "MULTI-REF", TotalPrice = 360.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var roomBooking1 = new RoomBooking { Booking_Id = booking.BookingId, Room_Id = room1.RoomId, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3) };
            var roomBooking2 = new RoomBooking { Booking_Id = booking.BookingId, Room_Id = room2.RoomId, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3) };
            context.RoomBookings.Add(roomBooking1);
            context.RoomBookings.Add(roomBooking2);
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetBookingByReference("MULTI-REF");

            Assert.NotNull(result);
            Assert.Equal(360.0, result.TotalPrice);
            Assert.Equal(2, result.RoomBookings.Count);
            Assert.Contains(result.RoomBookings, rb => rb.RoomNumber == "200");
            Assert.Contains(result.RoomBookings, rb => rb.RoomNumber == "201");
        }

        [Fact]
        public async Task GetBookingByReference_WithNullRoomBookings_ReturnsBooking()
        {
            // Test GetBookingByReference when RoomBookings is null
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var customer = new Customer { FirstName = "Bob", LastName = "Jones", Email = "bob@test", Phone = "1" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "NULL-ROOMS", TotalPrice = 0.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetBookingByReference("NULL-ROOMS");

            Assert.NotNull(result);
            Assert.Equal("Bob Jones", result.CustomerName);
            Assert.Empty(result.RoomBookings);
        }

        [Fact]
        public async Task GetBookingByReference_WithMultipleGuests_ReturnsAllGuests()
        {
            // Test GetBookingByReference with multiple guests in room booking
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var customer = new Customer { FirstName = "Leader", LastName = "Person", Email = "leader@test", Phone = "1" };
            context.Customers.Add(customer);
            context.SaveChanges();

            var hotel = new Hotel { Name = "GroupHotel", Address = "X", Phone = "0" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 300, PricePerNight = 200.0, Capacity = 4 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var booking = new Booking { Customer_Id = customer.CustomerId, BookingReference = "GROUP-REF", TotalPrice = 600.0 };
            context.Bookings.Add(booking);
            context.SaveChanges();

            var roomBooking = new RoomBooking { Booking_Id = booking.BookingId, Room_Id = room.RoomId, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3) };
            context.RoomBookings.Add(roomBooking);
            context.SaveChanges();

            // Add multiple guests
            var guests = new List<string> { "Guest1", "Guest2", "Guest3", "Guest4" };
            foreach (var guestName in guests)
            {
                var guest = new Customer { FirstName = guestName, LastName = "Attendee", Email = $"{guestName}@test", Phone = "0" };
                context.Customers.Add(guest);
                context.SaveChanges();
                var guestBooking = new GuestBooking { RoomBooking_Id = roomBooking.RoomBookingId, GuestId = guest.CustomerId };
                context.GuestBookings.Add(guestBooking);
            }
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetBookingByReference("GROUP-REF");

            Assert.NotNull(result);
            Assert.Single(result.RoomBookings);
            Assert.Equal(4, result.RoomBookings.First().Guests.Count);
        }

        [Fact]
        public async Task CreateBooking_LongStay_CalculatesCorrectPrice()
        {
            // Test long stay (10+ nights)
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "LongStayHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, PricePerNight = 50.0, Capacity = 1 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(11); // 11 nights

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Long", LastName = "Stayer", Email = "long@stay.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 101,
                        PricePerNight = 50.0,
                        Capacity = 1,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Long", LastName = "Stayer", Email = "long@stay.com", Phone="555" }
                        }
                    }
                },
                StartDate = startDate,
                EndDate = endDate
            };

            var response = await service.CreateBooking(request);

            Assert.NotNull(response);
            Assert.NotNull(response.bookingResponse.BookingReference);
            // 11 nights * 50 = 550
            Assert.Equal(550.0, response.bookingResponse.TotalPrice);
        }

        [Fact]
        public async Task CreateBooking_DifferentRoomPrices_CalculatesCorrectTotal()
        {
            // Test booking with rooms of different prices
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "VariedPriceHotel", Address = "Addr", Phone = "123" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room1 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 101, PricePerNight = 50.0, Capacity = 1 };
            var room2 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 102, PricePerNight = 100.0, Capacity = 2 };
            var room3 = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 103, PricePerNight = 150.0, Capacity = 3 };
            context.Rooms.Add(room1);
            context.Rooms.Add(room2);
            context.Rooms.Add(room3);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Multi", LastName = "Price", Email = "multi@price.com", Phone = "555" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 101,
                        PricePerNight = 50.0,
                        Capacity = 1,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Guest", LastName = "One", Email = "g1@price.com", Phone="555" }
                        }
                    },
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 102,
                        PricePerNight = 100.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Guest", LastName = "Two", Email = "g2@price.com", Phone="555" }
                        }
                    },
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 103,
                        PricePerNight = 150.0,
                        Capacity = 3,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Guest", LastName = "Three", Email = "g3@price.com", Phone="555" }
                        }
                    }
                },
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var response = await service.CreateBooking(request);

            Assert.NotNull(response);
            Assert.NotNull(response.bookingResponse.BookingReference);
            // (50 + 100 + 150) * 2 nights = 600
            Assert.Equal(600.0, response.bookingResponse.TotalPrice);
            Assert.Equal(3, response.bookingResponse.RoomBookings.Count);
        }

        [Fact]
        public async Task GetBookingByReference_EmptyReference_ThrowsException()
        {
            // Test GetBookingByReference with empty string
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByReference(""));

            Assert.Contains("Booking not found", ex.Message);
        }

        [Fact]
        public async Task GetBookingByReference_NullReference_ThrowsException()
        {
            // Test GetBookingByReference with null reference
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByReference(null!));
            Assert.NotNull(ex);
        }

        [Fact]
        public async Task CreateBooking_UpdatesCustomerIfExists()
        {
            // Test that existing customer details are updated when rebooking
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var existing = new Customer { FirstName = "Old", LastName = "Name", Email = "update@test.com", Phone = "1" };
            context.Customers.Add(existing);
            context.SaveChanges();

            var hotel = new Hotel { Name = "UpdateHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 10, PricePerNight = 20.0, Capacity = 1 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var service = CreateService(context);

            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Updated", LastName = "Name", Email = "update@test.com", Phone = "1" },
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
                            new CustomerViewModel{ FirstName = "Updated", LastName = "Name", Email = "update@test.com", Phone="1" }
                        }
                    }
                },
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var resp = await service.CreateBooking(request);

            Assert.NotNull(resp.bookingResponse.BookingReference);
            Assert.Equal("Updated Name", resp.bookingResponse.CustomerName);
            Assert.Single(context.Customers.Where(c => c.Email == "update@test.com"));
        }

        [Fact]
        public async Task CreateBooking_AdjacentBookings_AllowsBackToBackStays()
        {
            // Test that adjacent (non-overlapping) bookings are allowed
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateInMemoryContext(dbName);

            var hotel = new Hotel { Name = "AdjacentHotel", Address = "", Phone = "" };
            context.Hotels.Add(hotel);
            context.SaveChanges();

            var room = new Room { Hotel_Id = hotel.HotelId, RoomNumber = 50, PricePerNight = 30.0, Capacity = 2 };
            context.Rooms.Add(room);
            context.SaveChanges();

            var customer1 = new Customer { FirstName = "First", LastName = "Guest", Email = "first@guest", Phone = "0" };
            context.Customers.Add(customer1);
            context.SaveChanges();

            var booking1 = new Booking { Customer_Id = customer1.CustomerId, BookingReference = "FIRST", TotalPrice = 60.0 };
            context.Bookings.Add(booking1);
            context.SaveChanges();

            var roomBooking1 = new RoomBooking
            {
                Booking_Id = booking1.BookingId,
                Room_Id = room.RoomId,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };
            context.RoomBookings.Add(roomBooking1);
            context.SaveChanges();

            var service = CreateService(context);

            // Book same room starting right after previous booking ends
            var request = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "Second", LastName = "Guest", Email = "second@guest", Phone = "0" },
                Hotel = new HotelBookingViewModel { Name = hotel.Name },
                Rooms = new List<RoomBookingViewModel>
                {
                    new RoomBookingViewModel
                    {
                        HotelName = hotel.Name,
                        RoomNumber = 50,
                        PricePerNight = 30.0,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel>
                        {
                            new CustomerViewModel{ FirstName = "Second", LastName = "Guest", Email = "second@guest", Phone="0" }
                        }
                    }
                },
                // Starts exactly when first booking ends
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(4)
            };

            var resp = await service.CreateBooking(request);

            // Should be successful as these don't overlap
            Assert.NotNull(resp.bookingResponse.BookingReference);
            var roomBookingsCount = context.RoomBookings.Count(rb => rb.Room_Id == room.RoomId);
            Assert.Equal(2, roomBookingsCount);
        }
    }
}