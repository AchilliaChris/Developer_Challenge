using DataAccess;

namespace DeveloperChallenge
{
    public class RoomBookingService : IRoomBookingService
    {
        public RoomBookingService() { }

        public async Task<bool> RoomBooked(Room room, DateTime startDate, DateTime endDate)
        {
            if (room.RoomBookings != null)
                return room.RoomBookings.Any(b => startDate.Date >= b.StartDate.Date && startDate.Date <= b.EndDate.Date || endDate.Date >= b.StartDate.Date && endDate.Date <= b.EndDate.Date || b.StartDate.Date >= startDate.Date && b.StartDate.Date <= endDate.Date);
            else return false; // there are no bookings for this room
        }
    }
}
