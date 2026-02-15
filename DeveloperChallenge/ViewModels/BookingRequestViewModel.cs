using DeveloperChallenge.Validators;
using System.ComponentModel.DataAnnotations;

namespace DeveloperChallenge.ViewModels
{
    public class BookingRequestViewModel
    {
        [Required]
        public CustomerViewModel Customer { get; set; }
        [Required]
        public HotelBookingViewModel Hotel { get; set; }
        [Required]
        public List<RoomBookingViewModel> Rooms { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [FutureDate()]
        public DateTime StartDate { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [DateGreater("StartDate")]
        public DateTime EndDate { get; set; }
    }
}
