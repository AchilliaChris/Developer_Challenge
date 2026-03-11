using DataAccess;

namespace DeveloperChallenge
{
    public interface IHotelService
    {
        Task<List<Hotel>> GetAvailableHotelRooms(DateTime startDate, DateTime endDate, int numberOfGuests);
        Task<List<Hotel>> GetHotelByName(string name);
    }
}