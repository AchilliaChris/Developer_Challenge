using DataAccess;
using DeveloperChallenge;
using DeveloperChallenge.ViewModels;
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

        [Fact]
        public async Task GetHotelByName_WhenNameIsEmpty_ReturnsEmptyArray()
        {
            // Arrange
            var name = string.Empty;
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
        public async Task GetHotelByName_WhenNameIsWhitespace_ForwardsToService()
        {
            // Arrange
            var name = "   ";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(new List<Hotel>());

            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(name), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_WhenNameHasSpecialCharacters_ForwardsToService()
        {
            // Arrange
            var name = "Hotel-@123!";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(new List<Hotel>());

            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(name), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_ReturnsSingleHotel()
        {
            // Arrange
            var name = "Marriott";
            var hotelEntities = new List<Hotel>
            {
                new Hotel { HotelId = 1, Name = "Marriott Downtown", Address = "123 Main St", Phone = "+1-555-0100" }
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
            Assert.Single(array);
            Assert.Equal("Marriott Downtown", array[0].Name);
        }

        [Fact]
        public async Task GetHotelByName_WithLargeNumberOfResults_ReturnsMappedArray()
        {
            // Arrange
            var name = "Hotel";
            var hotelEntities = Enumerable.Range(1, 50)
                .Select(i => new Hotel 
                { 
                    HotelId = i, 
                    Name = $"Hotel {i}", 
                    Address = $"Address {i}", 
                    Phone = $"+1-555-0{i:D3}" 
                })
                .ToList();

            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(hotelEntities);

            var controller = CreateController(hotelServiceMock);

            // Act
            var result = await controller.GetHotelByName(name);

            // Assert
            var array = result.ToArray();
            Assert.Equal(50, array.Length);
            Assert.All(array, hotel => Assert.NotNull(hotel.Name));
        }

        [Fact]
        public async Task GetHotelByName_VerifiesMappingIsApplied()
        {
            // Arrange
            var name = "TestHotel";
            var hotelEntities = new List<Hotel>
            {
                new Hotel { HotelId = 1, Name = "Test Hotel 1", Address = "A1", Phone = "P1" },
                new Hotel { HotelId = 2, Name = "Test Hotel 2", Address = "A2", Phone = "P2" }
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
            Assert.NotNull(array);
            Assert.NotEmpty(array);
            // Verify all returned items are HotelViewModels with mapped Name property
            Assert.All(array, vm => 
            {
                Assert.NotNull(vm);
                Assert.NotNull(vm.Name);
                Assert.NotEmpty(vm.Name);
            });
        }

        [Fact]
        public async Task GetHotelByName_WhenServiceThrowsArgumentException_ExceptionPropagates()
        {
            // Arrange
            var name = "invalid";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid hotel name"));

            var controller = CreateController(hotelServiceMock);

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await controller.GetHotelByName(name));
        }

        [Fact]
        public async Task GetHotelByName_WhenServiceThrowsTimeoutException_ExceptionPropagates()
        {
            // Arrange
            var name = "timeout-test";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("Service timeout"));

            var controller = CreateController(hotelServiceMock);

            // Act / Assert
            await Assert.ThrowsAsync<TimeoutException>(async () => await controller.GetHotelByName(name));
        }

        [Fact]
        public async Task GetHotelByName_CallsServiceExactlyOnce()
        {
            // Arrange
            var name = "UniqueHotel";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(It.IsAny<string>()))
                .ReturnsAsync(new List<Hotel>());

            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_ReturnsIEnumerable()
        {
            // Arrange
            var name = "IEnumerableTest";
            var hotelEntities = new List<Hotel>
            {
                new Hotel { HotelId = 1, Name = "Test", Address = "A", Phone = "P" }
            };

            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(hotelEntities);

            var controller = CreateController(hotelServiceMock);

            // Act
            var result = await controller.GetHotelByName(name);

            // Assert
            Assert.IsAssignableFrom<IEnumerable<HotelViewModel>>(result);
        }

        [Fact]
        public async Task GetHotelByName_WhenNameWithUnicodeCharacters_ForwardsToService()
        {
            // Arrange
            var name = "Hôtel Café ★";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(new List<Hotel>());

            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(name), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_WhenNameVeryLong_ForwardsToService()
        {
            // Arrange
            var name = new string('A', 500);
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(new List<Hotel>());

            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName(name), Times.Once);
        }

        [Fact]
        public async Task GetHotelByName_WhenServiceReturnsHotelsWithNullAddressAndPhone_StillMaps()
        {
            // Arrange
            var name = "NullFieldsHotel";
            var hotelEntities = new List<Hotel>
            {
                new Hotel { HotelId = 1, Name = "Hotel A", Address = null, Phone = null }
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
            Assert.Single(array);
            Assert.NotNull(array[0].Name);
        }

        [Fact]
        public async Task GetHotelByName_VerifiesCaseSensitivityForwarding()
        {
            // Arrange
            var name = "MixedCaseHotel";
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetHotelByName(name))
                .ReturnsAsync(new List<Hotel>());

            var controller = CreateController(hotelServiceMock);

            // Act
            var _ = await controller.GetHotelByName(name);

            // Assert
            hotelServiceMock.Verify(s => s.GetHotelByName("MixedCaseHotel"), Times.Once);
        }

        }
}
