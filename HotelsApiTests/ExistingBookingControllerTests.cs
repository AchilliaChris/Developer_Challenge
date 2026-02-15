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
    }
}
