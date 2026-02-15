using DataAccess;
using DeveloperChallenge;
using HotelsAPIs.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;


namespace HotelsApiTests
{
    public class HotelsControllerTests
    {
        private static HotelsController CreateController(
            Mock<IHotelService>? hotelService = null)
        {
            var hs = hotelService ?? new Mock<IHotelService>();
            var logger = NullLogger<HotelsController>.Instance;
            return new HotelsController(hs.Object, logger);
        }

        [Fact]
        public async Task GetHotelByName_ReturnsMappedHotels()
        {
            // Arrange
            var name = "Hilton";
            var hotelEntities = new List<Hotel>
            {
                new Hotel { HotelId = 1, Name = "Hilton Downtown", Address = "A", Phone = "P" },
                new Hotel { HotelId = 2, Name = "Hilton Uptown", Address = "B", Phone = "Q" }
            };

            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(hotelEntities);

         
            var controller = CreateController(hotelServiceMock);

            // Act
            var result = await controller.GetHotelByName(name);

            // Assert
            var array = result.ToArray();
            Assert.Equal(2, array.Length);
            Assert.Contains(array, h => h.Name == "Hilton Downtown");
            Assert.Contains(array, h => h.Name == "Hilton Uptown");
        }

        [Fact]
        public async Task GetHotelByName_WhenServiceReturnsEmpty_ReturnsEmptyArray()
        {
            // Arrange
            var name = "NoSuchHotel";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(new List<Hotel>());


            var controller = CreateController(hotelServiceMock);

            // Act
            var result = await controller.GetHotelByName(name);

            // Assert
            var array = result.ToArray();
            Assert.Empty(array);
        }

        [Fact]
        public async Task GetHotelByName_PassesNameToService()
        {
            // Arrange
            var name = "ExactMatchName";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(It.IsAny<string>()))
                .ReturnsAsync(new List<Hotel>());

           
            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(name), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_WhenNameIsNull_ForwardsNullToService()
        {
            // Arrange
            string? name = null;
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(null!))
                .ReturnsAsync(new List<Hotel>());

        
            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name!);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(null!), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_WhenServiceThrows_ExceptionPropagates()
        {
            // Arrange
            var name = "boom";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("svc-failed"));

            var controller = CreateController(hotelServiceMock);

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await controller.GetHotelByName(name));
        }


        }
}
