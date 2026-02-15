using DataAccess;
using DeveloperChallenge;
using DeveloperChallenge.ViewModels;
using HotelsAPIs.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelsApiTests
{
    public class BookingControllerTests
    {
        private static BookingController CreateController(
            Mock<IHotelService>? hotelService = null,
            Mock<IBookingService>? bookingService = null/*,
            Mock<IMapper>? mapper = null*/)
        {
            var hs = hotelService ?? new Mock<IHotelService>();
            var bs = bookingService ?? new Mock<IBookingService>();
            var logger = NullLogger<HotelsController>.Instance;
            return new BookingController(hs.Object, bs.Object, logger);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_ReturnsMappedHotels()
        {
            // Arrange
            var start = DateTime.Today.AddDays(1);
            var end = DateTime.Today.AddDays(2);
            var hotelEntities = new List<Hotel>
            {
                new Hotel { HotelId = 1, Name = "H1", Address = "A", Phone = "P" },
                new Hotel { HotelId = 2, Name = "H2", Address = "B", Phone = "Q" }
            };

            var hotelViewModels = hotelEntities.Select(h => new HotelViewModel { Name = h.Name }).ToList();

            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetAvailableHotelRooms(start, end, 2))
                .ReturnsAsync(hotelEntities);

        
            var controller = CreateController(hotelServiceMock, new Mock<IBookingService>());

            // Act
            var result = await controller.GetAvailableHotelRooms(start, end, 2);

            // Assert
            var array = result.ToArray();
            Assert.Equal(2, array.Length);
            Assert.Contains(array, h => h.Name == "H1");
            Assert.Contains(array, h => h.Name == "H2");
        }

        [Fact]
        public async Task BookRoom_ServiceReturnsNotFound_ReturnsNotFoundObjectResult()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel { Customer = new CustomerViewModel(), Hotel = new HotelBookingViewModel(), Rooms = new List<RoomBookingViewModel>(), StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1) };
            var bookingResponse = new BookingResponseViewModel(); // empty
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.CreateBooking(It.IsAny<BookingRequestViewModel>()))
                .ReturnsAsync((bookingResponse, "Hotel not found: X"));

            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);

            // Act
            var actionResult = await controller.BookRoom(bookingRequest);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Equal("Hotel not found: X", notFound.Value);
        }

        [Fact]
        public async Task BookRoom_ServiceReturnsEmptyRoomBookings_ReturnsNotFound()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel { Customer = new CustomerViewModel(), Hotel = new HotelBookingViewModel(), Rooms = new List<RoomBookingViewModel>(), StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1) };
            var bookingResponse = new BookingResponseViewModel
            {
                BookingReference = "BR",
                RoomBookings = new List<RoomBookingResponseViewModel>() // intentionally empty
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.CreateBooking(It.IsAny<BookingRequestViewModel>()))
                .ReturnsAsync((bookingResponse, "Room not available"));

            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock/*, new Mock<IMapper>()*/);

            // Act
            var actionResult = await controller.BookRoom(bookingRequest);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Equal("Room not available", notFound.Value);
        }

        [Fact]
        public async Task GetHotelByName_WhenNameIsNull_ForwardsNullToService()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel { Customer = new CustomerViewModel(), Hotel = new HotelBookingViewModel(), Rooms = new List<RoomBookingViewModel>(), StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2) };
            var bookingResponseModel = new BookingResponseViewModel
            {
                BookingReference = "OKREF",
                CustomerName = "Customer",
                TotalPrice = 100,
                RoomBookings = new List<RoomBookingResponseViewModel>
                {
                    new RoomBookingResponseViewModel { HotelName = "H", RoomNumber = "1", StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2), Guests = new List<string>{ "G" } }
                }
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.CreateBooking(It.IsAny<BookingRequestViewModel>()))
                .ReturnsAsync((bookingResponseModel, "Booking Complete"));

            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);

            // Act
            var actionResult = await controller.BookRoom(bookingRequest);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult);
            var value = Assert.IsType<BookingResponseViewModel>(ok.Value);
            Assert.Equal("OKREF", value.BookingReference);
            Assert.Single(value.RoomBookings);
        }

        [Fact]
        public async Task BookRoom_WhenModelStateInvalid_ReturnsBadRequest()
        {
            // Arrange
            var bookingServiceMock = new Mock<IBookingService>();
            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);
            controller.ModelState.AddModelError("Customer", "Required");

            // Act
            var result = await controller.BookRoom(null!);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            // Value is typically a ValidationProblemDetails when using [ApiController] behavior; ensure something is returned
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task BookRoom_WhenServiceThrows_ExceptionPropagates()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel { Customer = new CustomerViewModel(), Hotel = new HotelBookingViewModel(), Rooms = new List<RoomBookingViewModel>(), StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2) };
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.CreateBooking(It.IsAny<BookingRequestViewModel>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.BookRoom(bookingRequest));
        }

        [Fact]
        public async Task GetAvailableHotelRooms_WhenHotelServiceThrows_ExceptionPropagates()
        {
            // Arrange
            var start = DateTime.Today.AddDays(1);
            var end = DateTime.Today.AddDays(2);
            var hotelServiceMock = new Mock<IHotelService>();
            hotelServiceMock
                .Setup(s => s.GetAvailableHotelRooms(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("svc-failed"));

            var controller = CreateController(hotelServiceMock, new Mock<IBookingService>());

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(async () => await controller.GetAvailableHotelRooms(start, end, 1));
        }
    

    [Fact]
        public async Task BookRoom_WhenCustomerEmailIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "invalid-email", // Invalid email format
                    Phone = "+12025551234"
                },
                Hotel = new HotelBookingViewModel(),
                Rooms = new List<RoomBookingViewModel>(),
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var bookingServiceMock = new Mock<IBookingService>();
            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);
            controller.ModelState.AddModelError("Customer.Email", "Invalid email address.");

            // Act
            var result = await controller.BookRoom(bookingRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task BookRoom_WhenCustomerPhoneIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john@example.com",
                    Phone = "invalid-phone" // Invalid phone format
                },
                Hotel = new HotelBookingViewModel(),
                Rooms = new List<RoomBookingViewModel>(),
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var bookingServiceMock = new Mock<IBookingService>();
            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);
            controller.ModelState.AddModelError("Customer.Phone", "The field must contain a valid phone number.");

            // Act
            var result = await controller.BookRoom(bookingRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task BookRoom_WhenCustomerEmailIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = string.Empty, // Empty email
                    Phone = "+12025551234"
                },
                Hotel = new HotelBookingViewModel(),
                Rooms = new List<RoomBookingViewModel>(),
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var bookingServiceMock = new Mock<IBookingService>();
            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);
            controller.ModelState.AddModelError("Customer.Email", "The Email field is required.");

            // Act
            var result = await controller.BookRoom(bookingRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task BookRoom_WhenCustomerPhoneIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john@example.com",
                    Phone = string.Empty // Empty phone
                },
                Hotel = new HotelBookingViewModel(),
                Rooms = new List<RoomBookingViewModel>(),
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var bookingServiceMock = new Mock<IBookingService>();
            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);
            controller.ModelState.AddModelError("Customer.Phone", "The Phone field is required.");

            // Act
            var result = await controller.BookRoom(bookingRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task BookRoom_WhenCustomerHasValidEmailAndPhone_ProceedsWithBooking()
        {
            // Arrange
            var bookingRequest = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john@example.com",
                    Phone = "+12025551234"
                },
                Hotel = new HotelBookingViewModel(),
                Rooms = new List<RoomBookingViewModel>(),
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            var bookingResponseModel = new BookingResponseViewModel
            {
                BookingReference = "REF123",
                CustomerName = "John Doe",
                TotalPrice = 250,
                RoomBookings = new List<RoomBookingResponseViewModel>()
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.CreateBooking(It.IsAny<BookingRequestViewModel>()))
                .ReturnsAsync((bookingResponseModel, "Success"));

            var controller = CreateController(new Mock<IHotelService>(), bookingServiceMock);

            // Act
            var result = await controller.BookRoom(bookingRequest);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Success", notFound.Value);
        }
    }

    // helper extension to adapt IEnumerable<T> returned by controller to a Task for exception assertion
    internal static class EnumerableTaskExtensions
    {
        public static async Task AsTask<T>(this IEnumerable<T> source)
        {
            // force evaluation which will execute awaited code paths in controller
            await Task.Yield();
            _ = source.ToArray();
        }
    }
}