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

        [Fact]
        public async Task RoomBooked_WithManyBookings_CorrectlyIdentifiesOverlap()
        {
            // Test with 10+ bookings to verify performance and correctness
            var room = CreateRoom(
                (new DateTime(2025, 1, 1), new DateTime(2025, 1, 2)),
                (new DateTime(2025, 1, 5), new DateTime(2025, 1, 6)),
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 11)),
                (new DateTime(2025, 1, 15), new DateTime(2025, 1, 16)),
                (new DateTime(2025, 1, 20), new DateTime(2025, 1, 21)),
                (new DateTime(2025, 2, 1), new DateTime(2025, 2, 2)),
                (new DateTime(2025, 2, 10), new DateTime(2025, 2, 11)),
                (new DateTime(2025, 2, 20), new DateTime(2025, 2, 21)),
                (new DateTime(2025, 3, 1), new DateTime(2025, 3, 2))
            );

            // Overlap with 7th booking
            var result = await _service.RoomBooked(room, new DateTime(2025, 2, 10), new DateTime(2025, 2, 11));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_NoOverlapWithManyBookings_ReturnsFalse()
        {
            // Test with many bookings, querying gap between them
            var room = CreateRoom(
                (new DateTime(2025, 1, 1), new DateTime(2025, 1, 3)),
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 12)),
                (new DateTime(2025, 1, 20), new DateTime(2025, 1, 22))
            );

            // Query between 3rd and 4th booking (gap)
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 13), new DateTime(2025, 1, 19));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_OverlapAtEndOfMonthBoundary()
        {
            // Test month-end boundary conditions
            var room = CreateRoom((new DateTime(2025, 1, 30), new DateTime(2025, 2, 2)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 31), new DateTime(2025, 2, 1));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_NoOverlapSpanningMonths_ReturnsFalse()
        {
            var room = CreateRoom((new DateTime(2025, 1, 25), new DateTime(2025, 1, 28)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 2, 1), new DateTime(2025, 2, 5));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_OverlapSpanningYear()
        {
            // Test year boundary conditions
            var room = CreateRoom((new DateTime(2024, 12, 30), new DateTime(2025, 1, 3)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 1), new DateTime(2025, 1, 2));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_NoOverlapAroundYearBoundary_ReturnsFalse()
        {
            var room = CreateRoom((new DateTime(2024, 12, 28), new DateTime(2024, 12, 31)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 1), new DateTime(2025, 1, 5));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_OverlapWithVeryOldBooking()
        {
            // Test with historical dates
            var room = CreateRoom((new DateTime(2000, 1, 1), new DateTime(2000, 1, 5)));
            
            var result = await _service.RoomBooked(room, new DateTime(2000, 1, 3), new DateTime(2000, 1, 4));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_OverlapWithFarFutureBooking()
        {
            // Test with far future dates
            var room = CreateRoom((new DateTime(2100, 1, 1), new DateTime(2100, 1, 10)));
            
            var result = await _service.RoomBooked(room, new DateTime(2100, 1, 5), new DateTime(2100, 1, 8));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_LongBookingPeriod_OverlapDetected()
        {
            // Test with very long booking (100 days)
            var room = CreateRoom((new DateTime(2025, 1, 1), new DateTime(2025, 4, 10)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 2, 1), new DateTime(2025, 2, 28));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_SingleDayQueryAgainstLongBooking_OverlapDetected()
        {
            // Test single day query against long booking
            var room = CreateRoom((new DateTime(2025, 1, 1), new DateTime(2025, 3, 31)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 2, 15), new DateTime(2025, 2, 15));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ExactlyAtBookingStart_OverlapDetected()
        {
            // Test query starting exactly at existing booking start
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 10), new DateTime(2025, 1, 12));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_ExactlyAtBookingEnd_OverlapDetected()
        {
            // Test query ending exactly at existing booking end
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 13), new DateTime(2025, 1, 15));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_QueryBeforeAllBookings_ReturnsFalse()
        {
            // Test query completely before all bookings
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)),
                (new DateTime(2025, 2, 1), new DateTime(2025, 2, 5))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 1), new DateTime(2025, 1, 5));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_QueryAfterAllBookings_ReturnsFalse()
        {
            // Test query completely after all bookings
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)),
                (new DateTime(2025, 1, 20), new DateTime(2025, 1, 25))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 2, 1), new DateTime(2025, 2, 10));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_QueryBetweenConsecutiveBookings_ReturnsFalse()
        {
            // Test query in gap between consecutive bookings
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)),
                (new DateTime(2025, 1, 20), new DateTime(2025, 1, 25))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 16), new DateTime(2025, 1, 19));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_AsyncBehavior_CompletesSuccessfully()
        {
            // Test async behavior with await
            var room = CreateRoom((new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)));
            
            var task = _service.RoomBooked(room, new DateTime(2025, 1, 12), new DateTime(2025, 1, 13));
            Assert.NotNull(task);
            
            var result = await task;
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_MultipleConsecutiveCalls_AllCorrect()
        {
            // Test multiple consecutive calls with different queries
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 15)),
                (new DateTime(2025, 2, 1), new DateTime(2025, 2, 5))
            );
            
            var result1 = await _service.RoomBooked(room, new DateTime(2025, 1, 12), new DateTime(2025, 1, 13));
            var result2 = await _service.RoomBooked(room, new DateTime(2025, 1, 20), new DateTime(2025, 1, 25));
            var result3 = await _service.RoomBooked(room, new DateTime(2025, 2, 2), new DateTime(2025, 2, 3));
            
            Assert.True(result1);  // overlaps first
            Assert.False(result2); // gap between bookings
            Assert.True(result3);  // overlaps second
        }

        [Fact]
        public async Task RoomBooked_AllBookingsSameDay_OverlapDetected()
        {
            // Test with multiple bookings on same day
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 10)),
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 10))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 10), new DateTime(2025, 1, 10));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_VeryShortGap_GapNotOverlapping_ReturnsFalse()
        {
            // Test with single day gap between bookings
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 12)),
                (new DateTime(2025, 1, 14), new DateTime(2025, 1, 16))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 13), new DateTime(2025, 1, 13));
            Assert.False(result);
        }

        [Fact]
        public async Task RoomBooked_PartialOverlapOfFirstBooking()
        {
            // Test partial overlap starting in gap but ending in booking
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 12)),
                (new DateTime(2025, 1, 20), new DateTime(2025, 1, 25))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 5), new DateTime(2025, 1, 11));
            Assert.True(result);
        }

        [Fact]
        public async Task RoomBooked_PartialOverlapOfLastBooking()
        {
            // Test partial overlap starting in booking but ending in gap
            var room = CreateRoom(
                (new DateTime(2025, 1, 10), new DateTime(2025, 1, 12)),
                (new DateTime(2025, 1, 20), new DateTime(2025, 1, 25))
            );
            
            var result = await _service.RoomBooked(room, new DateTime(2025, 1, 21), new DateTime(2025, 2, 1));
            Assert.True(result);
        }
    }
}
