using DeveloperChallenge.ViewModels;

namespace DeveloperChallenge
{
    public interface IBookingService
    {
        Task<(BookingResponseViewModel bookingResponse, string message)> CreateBooking(BookingRequestViewModel BookingRequest);
        Task<BookingResponseViewModel> GetBookingByReference(string bookingReference);
    }
}