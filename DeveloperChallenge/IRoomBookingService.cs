using DataAccess;

namespace DeveloperChallenge
{
    public interface IRoomBookingService
    {
        Task<bool>  RoomBooked(Room room, DateTime startDate, DateTime endDate);
    }
}