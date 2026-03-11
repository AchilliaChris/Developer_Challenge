using DataAccess;
using DeveloperChallenge.ViewModels;

namespace DeveloperChallenge.Mappers
{
    public static class Mapper
    {
        public static HotelViewModel MapHotelToHotelViewModel(Hotel hotel)
        {
            return new HotelViewModel
            {
                Name = hotel.Name,
                Address = hotel.Address,
                Phone = hotel.Phone,
                Rooms = hotel.Rooms?.Select(MapRoomToRoomViewModel).ToList() ?? new List<RoomViewModel>()
            };
        }
        public static RoomViewModel MapRoomToRoomViewModel(Room room)
        {
            return new RoomViewModel
            {
                HotelName = room.Hotel?.Name ?? string.Empty,
                RoomType = ((RoomType)room.RoomTypeId).ToString(),
                RoomNumber = room.RoomNumber,
                PricePerNight = room.PricePerNight,
                Capacity = room.Capacity
            };
        }
    }
}
