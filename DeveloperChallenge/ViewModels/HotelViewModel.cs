namespace DeveloperChallenge.ViewModels
{
    public class HotelViewModel
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public List<RoomViewModel> Rooms { get; set; } = new();
    }
}
