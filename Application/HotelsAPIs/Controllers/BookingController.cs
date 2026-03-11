using DeveloperChallenge;
using DeveloperChallenge.Mappers;
using DeveloperChallenge.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelsAPIs.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IHotelService hotelService;
        private readonly IBookingService bookingService;
        private readonly ILogger<HotelsController> logger;
       // private readonly IMapper mapper;
        public BookingController(IHotelService _hotelService,
            IBookingService _bookingService,
            ILogger<HotelsController> _logger)
        {
            hotelService = _hotelService;
            bookingService = _bookingService;
            logger = _logger;
        }
        [Route("getavailable")]
        [HttpGet(Name = "GetAvailableHotelRooms")]
        public async Task<IEnumerable<HotelViewModel>> GetAvailableHotelRooms(DateTime startDate, DateTime endDate, int numberOfGuests)
        {
            var availableHotels = await hotelService.GetAvailableHotelRooms(startDate, endDate, numberOfGuests);
            return availableHotels.Select(hotel => Mapper.MapHotelToHotelViewModel(hotel))
                .ToArray();
        }

        [Route("bookroom")]
        [HttpPost(Name ="BookRoom")]
        public async Task<IActionResult> BookRoom([FromBody] BookingRequestViewModel bookingRequest)
        {
            if (bookingRequest == null || !ModelState.IsValid)
            {
                var errorMessage = "There was no Booking Request supplied to the booking service.";
                return BadRequest(errorMessage);
            }
            var bookingResponse = await bookingService.CreateBooking(bookingRequest);
            if (bookingResponse.bookingResponse == null || bookingResponse.bookingResponse.RoomBookings.Count == 0) { 
                return NotFound(bookingResponse.message);
            }
            return Ok(bookingResponse.bookingResponse);
        }
    }
}
