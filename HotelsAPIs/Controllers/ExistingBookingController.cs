using DeveloperChallenge;
using DeveloperChallenge.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelsAPIs.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExistingBookingController : ControllerBase
    {
        private readonly ILogger<ExistingBookingController> logger;
        private readonly IBookingService bookingService;

        public ExistingBookingController(ILogger<ExistingBookingController> _logger,
            IBookingService _bookingService)
        {
            logger = _logger;
            bookingService = _bookingService;
        }

        [Route("findbooking")]
        [HttpGet(Name = "GetBooking")]
        public async Task<BookingResponseViewModel> GetBooking(string BookingReference)
        {
            // TO DO: Implement booking retrieval logic here
            return await bookingService.GetBookingByReference(BookingReference);
        }
    }
}
