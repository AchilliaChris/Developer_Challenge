namespace DeveloperChallenge.ViewModels
{
    public class RoomViewModel
    {
        public string HotelName { get; set; } = null!;
        public string RoomType { get; set; } = null!;
        public int RoomNumber { get; set; }
        public double PricePerNight { get; set; }
        public int Capacity { get; set; } = 0;

    }
}
