using DeveloperChallenge.Validators;

namespace DeveloperChallenge.ViewModels
{
    public class RoomBookingViewModel
    {
        public string HotelName { get; set; } = null!;
        public string RoomType { get; set; } = null!;
        public int RoomNumber { get; set; }
        public double PricePerNight { get; set; }
        public int Capacity { get; set; } = 0;
        [GuestCapacity("Capacity")]
        public List<CustomerViewModel> Guests { get; set; }
    }
}
