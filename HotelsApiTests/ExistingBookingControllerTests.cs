using DeveloperChallenge;
using DeveloperChallenge.ViewModels;
using HotelsAPIs.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelsApiTests
{
    public class ExistingBookingControllerTests
    {
        private static ExistingBookingController CreateController(
            Mock<IBookingService>? bookingService = null)
        {
            var bs = bookingService ?? new Mock<IBookingService>();
            var logger = NullLogger<ExistingBookingController>.Instance;
            return new ExistingBookingController(logger, bs.Object);
        }

        [Fact]
        public async Task GetBooking_ServiceReturnsBooking_ReturnsSameBooking()
        {
            // Arrange
            var bookingReference = "ABC123";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Jane Doe",
                TotalPrice = 250.0
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.BookingReference, result.BookingReference);
            Assert.Equal(expected.CustomerName, result.CustomerName);
            Assert.Equal(expected.TotalPrice, result.TotalPrice);
        }

        [Fact]
        public async Task GetBooking_ServiceReturnsNull_ReturnsNull()
        {
            // Arrange
            var bookingReference = "NONEXISTENT";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync((BookingResponseViewModel?)null);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBooking_PassesBookingReferenceToService_VerifyCalledWithExactValue()
        {
            // Arrange
            var bookingReference = "REF-VERIFY";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ReturnsAsync(new BookingResponseViewModel());

            var controller = CreateController(bookingServiceMock);

            // Act
            await controller.GetBooking(bookingReference);

            // Assert
            bookingServiceMock.Verify(s => s.GetBookingByReference(bookingReference), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WithEmptyReference_CallsServiceWithEmptyStringAndReturnsResult()
        {
            // Arrange
            var bookingReference = string.Empty;
            var expected = new BookingResponseViewModel { BookingReference = bookingReference };
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bookingReference, result.BookingReference);
            bookingServiceMock.Verify(s => s.GetBookingByReference(bookingReference), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WithNullReference_CallsServiceWithNullAndReturnsResult()
        {
            // Arrange
            string? bookingReference = null;
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference((string?)null))
                .ReturnsAsync((BookingResponseViewModel?)null);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference!);

            // Assert
            Assert.Null(result);
            bookingServiceMock.Verify(s => s.GetBookingByReference((string?)null), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WhenServiceThrows_ExceptionPropagates()
        {
            // Arrange
            var bookingReference = "THROW";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("service failure"));

            var controller = CreateController(bookingServiceMock);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetBooking(bookingReference));
        }

        [Fact]
        public async Task GetBooking_WithWhitespaceReference_CallsServiceWithWhitespaceAndReturnsResult()
        {
            // Arrange
            var bookingReference = "   ";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync((BookingResponseViewModel?)null);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.Null(result);
            bookingServiceMock.Verify(s => s.GetBookingByReference(bookingReference), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WithSpecialCharactersInReference_CallsServiceAndReturnsResult()
        {
            // Arrange
            var bookingReference = "REF-@#$-2024";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "John Smith",
                TotalPrice = 500.0
            };
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bookingReference, result.BookingReference);
            bookingServiceMock.Verify(s => s.GetBookingByReference(bookingReference), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WithVeryLongReference_CallsServiceAndReturnsResult()
        {
            // Arrange
            var bookingReference = new string('A', 500);
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Customer",
                TotalPrice = 100.0
            };
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bookingReference, result.BookingReference);
        }

        [Fact]
        public async Task GetBooking_CallsServiceExactlyOnce()
        {
            // Arrange
            var bookingReference = "SINGLE-CALL";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ReturnsAsync((BookingResponseViewModel?)null);

            var controller = CreateController(bookingServiceMock);

            // Act
            await controller.GetBooking(bookingReference);

            // Assert
            bookingServiceMock.Verify(s => s.GetBookingByReference(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WhenServiceThrowsArgumentException_ExceptionPropagates()
        {
            // Arrange
            var bookingReference = "INVALID-ARG";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid booking reference format"));

            var controller = CreateController(bookingServiceMock);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => controller.GetBooking(bookingReference));
        }

        [Fact]
        public async Task GetBooking_WhenServiceThrowsTimeoutException_ExceptionPropagates()
        {
            // Arrange
            var bookingReference = "TIMEOUT-REF";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("Service timeout"));

            var controller = CreateController(bookingServiceMock);

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => controller.GetBooking(bookingReference));
        }

        [Fact]
        public async Task GetBooking_WithCaseSensitiveReference_ForwardsExactCaseToService()
        {
            // Arrange
            var bookingReference = "MixedCaseREF123";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ReturnsAsync(new BookingResponseViewModel { BookingReference = bookingReference });

            var controller = CreateController(bookingServiceMock);

            // Act
            await controller.GetBooking(bookingReference);

            // Assert
            bookingServiceMock.Verify(s => s.GetBookingByReference("MixedCaseREF123"), Times.Once);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsCompleteBooking_ReturnsAllProperties()
        {
            // Arrange
            var bookingReference = "COMPLETE";
            var expected = new BookingResponseViewModel
            {
                BookingReference = "COMPLETE",
                CustomerName = "Jane Smith",
                TotalPrice = 999.99,
                RoomBookings = new List<RoomBookingResponseViewModel>
                {
                    new RoomBookingResponseViewModel 
                    { 
                        HotelName = "Grand Hotel",
                        RoomNumber = "101",
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(3),
                        Guests = new List<string> { "Jane Smith", "John Smith" }
                    }
                }
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.BookingReference, result.BookingReference);
            Assert.Equal(expected.CustomerName, result.CustomerName);
            Assert.Equal(expected.TotalPrice, result.TotalPrice);
            Assert.NotNull(result.RoomBookings);
            Assert.Single(result.RoomBookings);
            Assert.Equal("Grand Hotel", result.RoomBookings.First().HotelName);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsBookingWithNullRoomBookings_ReturnsBooking()
        {
            // Arrange
            var bookingReference = "NO-ROOMS";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Customer",
                TotalPrice = 0.0,
                RoomBookings = null
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.RoomBookings);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsMultipleRoomBookings_ReturnsAllRooms()
        {
            // Arrange
            var bookingReference = "MULTI-ROOM";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Group Booking",
                TotalPrice = 1500.0,
                RoomBookings = new List<RoomBookingResponseViewModel>
                {
                    new RoomBookingResponseViewModel { HotelName = "Hotel A", RoomNumber = "101", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Guests = new List<string> { "Guest1" } },
                    new RoomBookingResponseViewModel { HotelName = "Hotel A", RoomNumber = "102", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Guests = new List<string> { "Guest2" } },
                    new RoomBookingResponseViewModel { HotelName = "Hotel B", RoomNumber = "201", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Guests = new List<string> { "Guest3" } }
                }
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.RoomBookings.Count);
            Assert.Contains(result.RoomBookings, r => r.RoomNumber == "101");
            Assert.Contains(result.RoomBookings, r => r.RoomNumber == "102");
            Assert.Contains(result.RoomBookings, r => r.HotelName == "Hotel B");
        }

        [Fact]
        public async Task GetBooking_WhenReferenceWithUnicodeCharacters_CallsServiceAndReturnsResult()
        {
            // Arrange
            var bookingReference = "REF-★-2024";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Unicode Test",
                TotalPrice = 250.0
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bookingReference, result.BookingReference);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsZeroPrice_ReturnsBooking()
        {
            // Arrange
            var bookingReference = "FREE-BOOKING";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Lucky Customer",
                TotalPrice = 0.0
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.0, result.TotalPrice);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsNegativePrice_ReturnsBooking()
        {
            // Arrange
            var bookingReference = "REFUND-BOOKING";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "Refunded Customer",
                TotalPrice = -100.0
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-100.0, result.TotalPrice);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsVeryHighPrice_ReturnsBooking()
        {
            // Arrange
            var bookingReference = "LUXURY-BOOKING";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = "VIP Customer",
                TotalPrice = 999999.99
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(999999.99, result.TotalPrice);
        }

        [Fact]
        public async Task GetBooking_WhenServiceReturnsNullCustomerName_StillReturnsBooking()
        {
            // Arrange
            var bookingReference = "NULL-NAME";
            var expected = new BookingResponseViewModel
            {
                BookingReference = bookingReference,
                CustomerName = null,
                TotalPrice = 150.0
            };

            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(bookingReference))
                .ReturnsAsync(expected);

            var controller = CreateController(bookingServiceMock);

            // Act
            var result = await controller.GetBooking(bookingReference);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.CustomerName);
            Assert.Equal(150.0, result.TotalPrice);
        }

        [Fact]
        public async Task GetBooking_WhenServiceThrowsGenericException_ExceptionPropagates()
        {
            // Arrange
            var bookingReference = "GENERIC-THROW";
            var bookingServiceMock = new Mock<IBookingService>();
            bookingServiceMock
                .Setup(s => s.GetBookingByReference(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Generic error"));

            var controller = CreateController(bookingServiceMock);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => controller.GetBooking(bookingReference));
        }
    }
}
