using DeveloperChallenge;
using DeveloperChallenge.Mappers;
using DeveloperChallenge.ViewModels;
using Microsoft.AspNetCore.Mvc;


namespace HotelsAPIs.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HotelsController : ControllerBase
    {
        private readonly IHotelService hotelService;
        private readonly ILogger<HotelsController> logger;
      //  private readonly IMapper mapper;
        public HotelsController(IHotelService _hotelService,
            ILogger<HotelsController> _logger) {
        hotelService = _hotelService;
            logger = _logger;
        }

        [Route("getbyname")]
        [HttpGet(Name = "GetByName")]
        public async Task<IActionResult> GetHotelByName(string name)
        {
            try
            {
                var hotel = await hotelService.GetHotelByName(name);
                var result = hotel.Select(hotel => Mapper.MapHotelToHotelViewModel(hotel)).ToArray();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
