using System.Net;
using System.Net.Http.Json;
using DeveloperChallenge.ViewModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotelsApiIntegrationTests
{
    [Collection("Sequential")] // This ensures we don't try to run other integration tests at the same time and cause conflicts with the database
    public class ExistingBookingApiTests : IClassFixture<WebApplicationFactory<HotelsAPIs.Controllers.ExistingBookingController>>
    {
        private readonly WebApplicationFactory<HotelsAPIs.Controllers.ExistingBookingController> _factory;

        public ExistingBookingApiTests(WebApplicationFactory<HotelsAPIs.Controllers.ExistingBookingController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetBooking_ReturnsInternalServerError_WhenBookingReferenceDoesNotExist()
        {
            var client = _factory.CreateClient();
            var bookingReference = "NONEXISTENT123";
            var response = await client.GetAsync($"/ExistingBooking/findbooking?BookingReference={bookingReference}");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task GetBooking_ReturnsBadRequest_WhenBookingReferenceIsEmpty()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/ExistingBooking/findbooking?BookingReference=");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetBooking_ReturnsBadRequest_WhenBookingReferenceIsMissing()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/ExistingBooking/findbooking");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetBooking_ReturnsInternalServerError_WhenBookingNotFound()
        {
            var client = _factory.CreateClient();
            var bookingReference = "REF-001";
            var response = await client.GetAsync($"/ExistingBooking/findbooking?BookingReference={bookingReference}");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
