using System.Net;
using System.Net.Http.Json;
using DeveloperChallenge.ViewModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotelsApiIntegrationTests
{
    [Collection("Sequential")]// This ensures we don't try to run other integration tests at the same time and cause conflicts with the database
    public class HotelsApiTests : IClassFixture<WebApplicationFactory<HotelsAPIs.Controllers.HotelsController>>
    {
        private readonly WebApplicationFactory<HotelsAPIs.Controllers.HotelsController> _factory;
        public HotelsApiTests(WebApplicationFactory<HotelsAPIs.Controllers.HotelsController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetHotelByName_ReturnsHotel_WhenNameIsValid()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/hotels/getbyname?name=TestHotel");
            response.EnsureSuccessStatusCode();
            var hotels = await response.Content.ReadFromJsonAsync<HotelViewModel[]>();
            Assert.NotNull(hotels);
            Assert.Contains(hotels, h => h.Name == "TestHotel");
        }

        [Fact]
        public async Task GetHotelByName_ReturnsEmpty_WhenHotelNotFound()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/hotels/getbyname?name=NoSuchHotel");
            response.EnsureSuccessStatusCode();
            var hotels = await response.Content.ReadFromJsonAsync<HotelViewModel[]>();
            Assert.NotNull(hotels);
            Assert.Empty(hotels);
        }

        [Fact]
        public async Task GetHotelByName_ReturnsBadRequest_WhenNameTooShort()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/hotels/getbyname?name=ab");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
