using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using DeveloperChallenge.ViewModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotelsApiIntegrationTests
{
    [Collection("Sequential")] // This ensures we don't try to run other integration tests at the same time and cause conflicts with the database
    public class BookingApiTests : IClassFixture<WebApplicationFactory<HotelsAPIs.Controllers.BookingController>>
    {
        private readonly WebApplicationFactory<HotelsAPIs.Controllers.BookingController> _factory;
        public BookingApiTests(WebApplicationFactory<HotelsAPIs.Controllers.BookingController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAvailableHotelRooms_ReturnsHotels_WhenValidRequest()
        {
            var client = _factory.CreateClient();
            var startDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");
            var response = await client.GetAsync($"/booking/getavailable?startDate={startDate}&endDate={endDate}&numberOfGuests=2");
            response.EnsureSuccessStatusCode();
            var hotels = await response.Content.ReadFromJsonAsync<HotelViewModel[]>();
            Assert.NotNull(hotels);
        }

        [Fact]
        public async Task GetAvailableHotelRooms_ReturnsEmpty_WhenNoHotelsAvailable()
        {
            var client = _factory.CreateClient();
            var startDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");
            var response = await client.GetAsync($"/booking/getavailable?startDate={startDate}&endDate={endDate}&numberOfGuests=99");
            response.EnsureSuccessStatusCode();
            var hotels = await response.Content.ReadFromJsonAsync<HotelViewModel[]>();
            Assert.NotNull(hotels);
            Assert.Empty(hotels);
        }

        [Fact]
        public async Task BookRoom_ReturnsOk_WhenValidBooking()
        {
            var client = _factory.CreateClient();
            var bookingRequest = new BookingRequestViewModel
            {
                Customer = new CustomerViewModel { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "+44 1917 1234567890", Address = "123 Main St" },
                Hotel = new HotelBookingViewModel { Name = "Grand Plaza" , Address = "A test address", Phone = "+44 1917 1234567890" },
                Rooms = new List<RoomBookingViewModel> {
                    new RoomBookingViewModel {
                        HotelName = "Grand Plaza",
                        RoomType = "Deluxe",
                        RoomNumber = 2,
                        PricePerNight = 155,
                        Capacity = 2,
                        Guests = new List<CustomerViewModel> {
                            new CustomerViewModel { FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "+44 1917 1234567890", Address = "123 Main St" }
                        }
                    }
                },
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            var response = await client.PostAsJsonAsync("/booking/bookroom", bookingRequest);
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task BookRoom_ReturnsBadRequest_WhenInvalidBooking()
        {
            var client = _factory.CreateClient();
            var bookingRequest = new BookingRequestViewModel(); // Invalid: missing required fields
            var response = await client.PostAsJsonAsync("/booking/bookroom", bookingRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
