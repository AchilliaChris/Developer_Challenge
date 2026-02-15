using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeveloperChallenge
{
    public class HotelService : IHotelService
    {
        private readonly HotelsDbContext context;
        private readonly ILogger<HotelService> logger;
        private readonly IRoomBookingService roomBookingService;
        private delegate Task<bool> RoomAvailabilityDelegate(Room room, DateTime start, DateTime end);
        private RoomAvailabilityDelegate roomAvailabilityDelegate;
        public HotelService(HotelsDbContext _context,
            IRoomBookingService _roomBookingService,
             ILogger<HotelService> _logger)
        {
            context = _context;
            roomBookingService = _roomBookingService;
            logger = _logger;
            roomAvailabilityDelegate = roomBookingService.RoomBooked;
        }


        public async Task<List<Hotel>> GetHotelByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Hotel name cannot be null or empty.", nameof(name));
            }

            if (name.Length < 3)
            {
                throw new ArgumentException("Hotel name must be at least 3 characters long.", nameof(name));
            }

            if (context.Hotels.Any(h => h.Name.ToLower().Equals(name.ToLower())))
            {
                return context.Hotels.Include(h => h.Rooms).Where(h => h.Name.ToLower().Equals(name.ToLower())).ToList();
            }
            else
            {
                return new List<Hotel>(); // we could look at ranking the available hotels and returning top 5
            }

        }

        public async Task<List<Hotel>> GetAvailableHotelRooms(DateTime startDate, DateTime endDate, int numberOfGuests)
        {
            if (startDate >= endDate)
            {
                throw new ArgumentException("startDate must be before endDate.");
            }

            // Load hotels with rooms and room bookings in one DB call
            var hotels = await context.Hotels
                .Include(h => h.Rooms)
                .ThenInclude(r => r.RoomBookings)
                .ToListAsync();

            var result = new List<Hotel>();

            foreach (var hotel in hotels)
            {
                if (hotel.Rooms == null || hotel.Rooms.Count == 0)
                {
                    hotel.Rooms = new List<Room>();
                    continue;
                }

                var availableRooms = new List<Room>();

                // Check each room asynchronously (awaiting the availability delegate)
                foreach (var room in hotel.Rooms)
                {
                    bool booked;
                    try
                    {
                        booked = await roomAvailabilityDelegate(room, startDate, endDate);
                    }
                    catch (Exception ex)
                    {
                        // Log and treat room as unavailable on error
                        logger?.LogWarning(ex, "Failed checking availability for room {RoomId} in hotel {HotelId}", room?.RoomId, hotel?.HotelId);
                        booked = true;
                    }

                    if (!booked)
                    {
                        availableRooms.Add(room);
                    }
                }

                hotel.Rooms = availableRooms;

                // Only include hotels that can accommodate the required number of guests
                if (hotel.Rooms.Sum(r => r.Capacity) >= numberOfGuests)
                {
                    result.Add(hotel);
                }
            }

            return result;
        }

    }
}
