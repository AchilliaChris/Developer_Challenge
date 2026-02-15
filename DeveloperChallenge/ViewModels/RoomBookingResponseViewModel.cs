
namespace DeveloperChallenge.ViewModels
{
    public class RoomBookingResponseViewModel
    {
        public string HotelName { get; set; } = null!;
        public string RoomNumber { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Guests { get; set; } = new();
    }
}
