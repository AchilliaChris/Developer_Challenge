using DataAccess;
using DeveloperChallenge.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqids;
using System.Security.Cryptography;

namespace DeveloperChallenge
{
    public class BookingService : IBookingService
    {
        private readonly SqidsEncoder<int> sqids;
        private readonly HotelsDbContext context;
        private readonly ILogger<BookingService> logger;
        IRoomBookingService roomBookingService;
        public BookingService(SqidsEncoder<int> _sqids,
            HotelsDbContext _context,
             ILogger<BookingService> _logger,
             IRoomBookingService _roomBookingService)
        {
            sqids = _sqids;
            context = _context;
            logger = _logger;
            roomBookingService = _roomBookingService;
        }

        public async Task<BookingResponseViewModel> GetBookingByReference(string bookingReference)
        {
            var booking = await context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.RoomBookings)
                    .ThenInclude(rb => rb.Room)
                        .ThenInclude(r => r.Hotel)
                .Include(b => b.RoomBookings)
                    .ThenInclude(rb => rb.GuestBookings)
                        .ThenInclude(gb => gb.Guest)
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);
            if (booking == null)
            {
                logger.LogError($"Booking not found: {bookingReference}");
                throw new Exception($"Booking not found: {bookingReference}");
            }
            var bookingResponse = new BookingResponseViewModel
            {
                BookingReference = booking.BookingReference,
                CustomerName = $"{booking.Customer.FirstName} {booking.Customer.LastName}",
                TotalPrice = booking.TotalPrice,
                RoomBookings = booking.RoomBookings.Select(rb => new RoomBookingResponseViewModel
                {
                    HotelName = rb.Room.Hotel.Name,
                    RoomNumber = rb.Room.RoomNumber.ToString(),
                    StartDate = rb.StartDate,
                    EndDate = rb.EndDate,
                    Guests = rb.GuestBookings.Select(gb => $"{gb.Guest.FirstName} {gb.Guest.LastName}").ToList()
                }).ToList()
            };
            return bookingResponse;
        }

        public async Task<(BookingResponseViewModel bookingResponse, string message)> CreateBooking(BookingRequestViewModel BookingRequest)
        {
            BookingResponseViewModel bookingResponse = new BookingResponseViewModel();

            Customer customer = GetCustomer(BookingRequest.Customer);
            var hotel = context.Hotels.FirstOrDefault(h => h.Name == BookingRequest.Hotel.Name);
            if (hotel == null)
            {
                var errorMessage = $"Hotel not found: {BookingRequest.Hotel.Name}";
                logger.LogError(errorMessage);
                return (bookingResponse, errorMessage);
            }

            foreach (var room in BookingRequest.Rooms)
            {
                var roomEntity = context.Rooms.FirstOrDefault(r => r.Hotel_Id == hotel.HotelId && r.RoomNumber == room.RoomNumber);
                if (roomEntity == null)
                {
                    var errorMessage = $"Room not found: Hotel '{room.HotelName}', Room Number '{room.RoomNumber}'";
                    logger.LogError(errorMessage);
                    return (bookingResponse, errorMessage);
                }
                if (await roomBookingService.RoomBooked(roomEntity, BookingRequest.StartDate, BookingRequest.EndDate))
                {
                    var errorMessage = $"Room not available: Hotel '{room.HotelName}', Room Number '{room.RoomNumber}'";
                    logger.LogError(errorMessage);
                    return (bookingResponse, errorMessage);
                }
            }
            /* This assumes the EndDate is the last night of occupation and you actually check out the following morning */
            int numberOfDays = (BookingRequest.EndDate - BookingRequest.StartDate).Days + 1;
            double totalPrice = 0;
            foreach (var room in BookingRequest.Rooms)
            {
                totalPrice += room.PricePerNight * numberOfDays;
            }
            var bookingReference = GetBookingReference();
            bookingResponse.BookingReference = bookingReference;
            bookingResponse.CustomerName = $"{BookingRequest.Customer.FirstName} {BookingRequest.Customer.LastName}";
            bookingResponse.TotalPrice = totalPrice;
            var booking = new Booking
            {
                Customer_Id = customer.CustomerId,
                BookingReference = bookingReference,
                TotalPrice = totalPrice,
            };

            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            foreach (var room in BookingRequest.Rooms)
            {

                var roomBookingResponse = await BookRoom(room, hotel.HotelId, booking.BookingId, BookingRequest.StartDate, BookingRequest.EndDate);
                if (roomBookingResponse != null)
                {
                    bookingResponse.RoomBookings.Add(roomBookingResponse);
                }
            }

            await context.SaveChangesAsync();

            return (bookingResponse, "Booking Complete");
        }

        private async Task<RoomBookingResponseViewModel> BookRoom(RoomBookingViewModel room, int hotelId, int bookingId, DateTime StartDate, DateTime EndDate)
        {
            var roomEntity = context.Rooms.FirstOrDefault(r => r.Hotel_Id == hotelId && r.RoomNumber == room.RoomNumber);

            if (roomEntity != null)
            {

                var roomBooking = new RoomBooking
                {
                    Booking_Id = bookingId,
                    Room_Id = roomEntity.RoomId,
                    StartDate = StartDate,
                    EndDate = EndDate
                };
                context.RoomBookings.Add(roomBooking);
                await context.SaveChangesAsync();
                foreach (var guestViewModel in room.Guests)
                {
                    var guest = GetCustomer(guestViewModel);
                    var guestBooking = new GuestBooking
                    {
                        RoomBooking_Id = roomBooking.RoomBookingId,
                        GuestId = guest.CustomerId
                    };
                    context.GuestBookings.Add(guestBooking);

                }

                RoomBookingResponseViewModel model = new RoomBookingResponseViewModel
                {
                    HotelName = room.HotelName,
                    RoomNumber = room.RoomNumber.ToString(),
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Guests = room.Guests.Select(g => $"{g.FirstName} {g.LastName}").ToList()
                };
                return model;
            }
            else
            {
                var errorMessage = $"Room not found: Hotel '{room.HotelName}', Room Number '{room.RoomNumber}'";
                logger.LogError(errorMessage);
            }
            return null;
        }
        private Customer GetCustomer(CustomerViewModel customerViewModel)
        {
            var customer = context.Customers
                .FirstOrDefault(c => c.Email == customerViewModel.Email);
            if (customer == null)
            {
                customer = new Customer
                {
                    FirstName = customerViewModel.FirstName,
                    LastName = customerViewModel.LastName,
                    Email = customerViewModel.Email,
                    Phone = customerViewModel.Phone
                };
                context.Customers.Add(customer);
                context.SaveChanges();
            }
            return customer;
        }

        private string GetBookingReference()
        {
            string bookingReference = GenerateBookingReference();
            while (context.Bookings.Any(b => b.BookingReference == bookingReference))
            {
                bookingReference = GenerateBookingReference();
            }
            return bookingReference;
        }
        private string GenerateBookingReference()
        {
            int a = RandomNumberGenerator.GetInt32(int.MaxValue);
            int b = RandomNumberGenerator.GetInt32(int.MaxValue);
            int c = RandomNumberGenerator.GetInt32(int.MaxValue);
            return sqids.Encode(a, b, c); // todo check for duplicates in dB
        }
    }
}
