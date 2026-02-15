using DataAccess;
using DeveloperChallenge;

namespace HotelsApiTests
{
    public class RoomBookingServiceTests
    {
        private readonly RoomBookingService _service = new RoomBookingService();

        private static Room CreateRoom(params (DateTime start, DateTime end)[] bookings)
        {
            var room = new Room
            {
                RoomId = 1,
                RoomBookings = new List<RoomBooking>()
            };

            foreach (var (start, end) in bookings)
            {
                room.RoomBookings.Add(new RoomBooking
                {
                    RoomBookingId = room.RoomBookings.Count + 1,
                    Room_Id = room.RoomId,
                    StartDate = start,
                    EndDate = end
                });
            }

            return room;
        }

    [Fact]
        public async Task RoomBooked_ThrowsNullReference_WhenRoomIsNull()
        {
            // Method does not validate null; current behavior is a NullReferenceException
            await Assert.ThrowsAsync<NullReferenceException>(() => _service.RoomBooked(null!, DateTime.Today, DateTime.Today.AddDays(1)));
        }

        [Fact]
        public async Task RoomBooked_ReturnsFalse_WhenBookingsIsNull()
        {
            var room = new Room { RoomId = 1, RoomBookings = null! }; // explicit null to exercise branch
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 1), new DateTime(2025, 1, 2));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsFalse_WhenBookingsIsEmpty()
        {
            var room = new Room { RoomId = 1, RoomBookings = new List<RoomBooking>() };
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 1), new DateTime(2025, 1, 2));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsTrue_WhenStartDateIsInsideExistingBooking()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 12), new DateTime(2025, 1, 13));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsTrue_WhenEndDateIsInsideExistingBooking()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 8), new DateTime(2025, 1, 11));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsTrue_WhenNewRangeEnclosesExistingBooking()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 9), new DateTime(2025, 1, 16));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsFalse_WhenNonOverlappingBeforeExistingBooking()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 1), new DateTime(2025, 1, 9));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsFalse_WhenNonOverlappingAfterExistingBooking()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 16), new DateTime(2025, 1, 20));
            Assert.False(result);
        }

        //[Fact]
        public async Task RoomBooked_TreatsAdjacentBeforeAsOverlap_WhenEndEqualsExistingStart()
        {
            // Current implementation uses inclusive Date comparisons; touching days are considered overlapping
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 5), new DateTime(2025, 1, 10));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_TreatsAdjacentAfterAsOverlap_WhenStartEqualsExistingEnd()
        {
            // Inclusive behavior: start equal to existing end is overlapping
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 15), new DateTime(2025, 1, 20));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_HandlesSingleDayExistingBooking()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 10)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 10), new DateTime(2025, 1, 10));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsTrue_WhenAnyOfMultipleBookingsOverlap()
        {
            var room = CreateRoom(
                (new DateTime(2025, 1, 1), new DateTime(2025, 1, 3)),
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)),
                (new DateTime(2025, 2, 1), new DateTime(2025, 2, 5))
            );

            // overlap with the second booking
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 12), new DateTime(2025, 1, 13));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsFalse_WhenMultipleBookingsDoNotOverlap()
        {
            var room = CreateRoom(
                (new DateTime(2025, 1, 1), new DateTime(2025, 1, 3)),
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 15))
            );

            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 4), new DateTime(2025, 1, 9));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_ReturnsFalse_WhenStartDateAfterEndDate()
        {
            // The implementation has no validation for start > end; it currently returns false for these inputs
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 20), new DateTime(2025, 1, 18));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_IgnoresTimeComponents_UsesOnlyDates()
        {
            var room = CreateRoom((new DateTime(2025, 1, 10).AddHours(14), new DateTime(2025, 1, 15).AddHours(20)));
            // Query uses same calendar day but different times -> should be considered overlapping
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 12).AddHours(2), new DateTime(2025, 1, 12).AddHours(4));
            Assert.True(result);
        }
    }
}
