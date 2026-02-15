
namespace DeveloperChallenge.ViewModels
{
    public class BookingResponseViewModel
    {
        public string CustomerName { get; set; } = null!;
        public string BookingReference { get; set; } = null!;
        public double TotalPrice { get; set; }
        public List<RoomBookingResponseViewModel> RoomBookings { get; set; } = new();
    }
}
