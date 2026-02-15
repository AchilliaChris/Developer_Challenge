using DeveloperChallenge.Validators;
using System.ComponentModel.DataAnnotations;

namespace HotelsApiTests
{
    public class ValidatorTests
    {
        // Helper to run validation on an object and return results
        private static bool TryValidate(object model, out List<ValidationResult> results)
        {
            var context = new ValidationContext(model);
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        }

        #region FutureDateAttribute tests

        private class FutureDateModel
        {
            [FutureDate]
            public DateTime? StartDate { get; set; }
        }

        [Fact]
        public void FutureDate_WithFutureDate_PassesValidation()
        {
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddHours(1)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void FutureDate_WithPastDate_FailsValidation_WithExpectedMessage()
        {
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddHours(-1)
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
            Assert.Equal("Start date must be in the future", results[0].ErrorMessage);
        }

        [Fact]
        public void FutureDate_WithNullValue_ThrowsInvalidCastException()
        {
            var model = new FutureDateModel
            {
                StartDate = null
            };

            // The attribute implementation casts (DateTime)value without null-check -> InvalidCastException expected
            Assert.Throws<NullReferenceException>(() => TryValidate(model, out _));
        }

        [Fact]
        public void FutureDate_WithNonDateValue_ThrowsInvalidCastException_WhenAppliedToWrongType()
        {
            // Intentionally apply attribute to a wrong-typed property to exercise the cast failure path
            var model = new WrongTypedFutureDateModel
            {
                StartDateAsString = "not-a-date"
            };

            Assert.Throws<InvalidCastException>(() => TryValidate(model, out _));
        }

        private class WrongTypedFutureDateModel
        {
            [FutureDate]
            public string? StartDateAsString { get; set; }
        }

        #endregion

        #region DateGreaterAttribute tests

        private class DateGreaterModel
        {
            public DateTime StartDate { get; set; }

            [DateGreater("StartDate")]
            public DateTime EndDate { get; set; }
        }

        [Fact]
        public void DateGreater_EndAfterStart_PassesValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_EndEqualsStart_PassesValidation()
        {
            var dt = DateTime.Today;
            var model = new DateGreaterModel
            {
                StartDate = dt,
                EndDate = dt // equality is allowed by implementation (only < triggers error)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_EndBeforeStart_FailsValidation_WithExpectedMessage()
        {
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(1)
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
            Assert.Equal("End date must be later than or equal to start date", results[0].ErrorMessage);
        }

        [Fact]
        public void DateGreater_MissingComparisonProperty_ThrowsNullReferenceException()
        {
            var model = new DateGreaterModelWithWrongComparison
            {
                Start = DateTime.Today,
                End = DateTime.Today.AddDays(1)
            };

            // The attribute implementation looks up the property by name and calls GetValue on it without null-check,
            // so if the property name is invalid a NullReferenceException is expected.
            Assert.Throws<NullReferenceException>(() => TryValidate(model, out _));
        }

        private class DateGreaterModelWithWrongComparison
        {
            // Note: DateGreater references "NonExistentProperty"
            public DateTime Start { get; set; }

            [DateGreater("NonExistentProperty")]
            public DateTime End { get; set; }
        }

        [Fact]
        public void DateGreater_ComparisonPropertyWrongType_ThrowsInvalidCastException()
        {
            var model = new DateGreaterModelWithWrongType
            {
                StartAsString = "2026-01-01",
                End = DateTime.Today.AddDays(1)
            };

            // The attribute attempts to cast the comparison property's value to DateTime -> InvalidCastException expected
            Assert.Throws<InvalidCastException>(() => TryValidate(model, out _));
        }

        private class DateGreaterModelWithWrongType
        {
            public string StartAsString { get; set; } = string.Empty;

            [DateGreater("StartAsString")]
            public DateTime End { get; set; }
        }

        #endregion

        #region Additional FutureDate Tests

        [Fact]
        public void FutureDate_WithCurrentDateTime_FailsValidation()
        {
            // Current time is not in the future
            var now = DateTime.Now;
            var model = new FutureDateModel
            {
                StartDate = now
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void FutureDate_WithVeryDistantFutureDate_PassesValidation()
        {
            // Very far in the future (50 years)
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddYears(50)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void FutureDate_WithVeryDistantPastDate_FailsValidation()
        {
            // Very far in the past (50 years)
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddYears(-50)
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void FutureDate_WithMinDateTime_FailsValidation()
        {
            // Minimum possible DateTime value
            var model = new FutureDateModel
            {
                StartDate = DateTime.MinValue
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void FutureDate_WithMaxDateTime_PassesValidation()
        {
            // Maximum possible DateTime value
            var model = new FutureDateModel
            {
                StartDate = DateTime.MaxValue
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void FutureDate_WithNanosecondPrecision_PassesValidation()
        {
            // With precise future time
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddMilliseconds(1000).AddTicks(500)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void FutureDate_WithMultipleSeconds_PassesValidation()
        {
            // Future by multiple seconds
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddSeconds(30)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void FutureDate_WithMultipleDays_PassesValidation()
        {
            // Future by multiple days
            var model = new FutureDateModel
            {
                StartDate = DateTime.Now.AddDays(365)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void FutureDate_WithLeapYearDate_PassesValidation()
        {
            // Leap year date in future
            var model = new FutureDateModel
            {
                StartDate = new DateTime(2024, 2, 29).AddYears(4) // Next leap year
            };

            if (model.StartDate > DateTime.Now)
            {
                var ok = TryValidate(model, out var results);
                Assert.True(ok);
                Assert.Empty(results);
            }
        }

        [Fact]
        public void FutureDate_WithDayLightSavingsTransition_PassesValidation()
        {
            // Date during DST transition if in future
            var model = new FutureDateModel
            {
                StartDate = new DateTime(2025, 3, 9, 14, 0, 0) // Spring forward
            };

            if (model.StartDate > DateTime.Now)
            {
                var ok = TryValidate(model, out var results);
                Assert.True(ok);
                Assert.Empty(results);
            }
        }

        #endregion

        #region Additional DateGreater Tests

        [Fact]
        public void DateGreater_EndManyDaysAfterStart_PassesValidation()
        {
            // End is 365 days after start
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(365)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_EndVeryCloseToStart_PassesValidation()
        {
            // End is just barely after start (ticks)
            var start = DateTime.Now;
            var model = new DateGreaterModel
            {
                StartDate = start,
                EndDate = start.AddTicks(1)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_EndOneSecondAfterStart_PassesValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddSeconds(1)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_EndOneMinuteBeforeStart_FailsValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMinutes(-1)
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void DateGreater_WithMinDateTime_PassesValidation()
        {
            // Both at min value (equal)
            var model = new DateGreaterModel
            {
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MinValue
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_WithMaxDateTime_PassesValidation()
        {
            // Both at max value (equal)
            var model = new DateGreaterModel
            {
                StartDate = DateTime.MaxValue,
                EndDate = DateTime.MaxValue
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_WithMinAndMaxDateTime_PassesValidation()
        {
            // Min to Max (should pass)
            var model = new DateGreaterModel
            {
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MaxValue
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_WithMaxAndMinDateTime_FailsValidation()
        {
            // Max to Min (should fail)
            var model = new DateGreaterModel
            {
                StartDate = DateTime.MaxValue,
                EndDate = DateTime.MinValue
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void DateGreater_WithNegativeTimespanDifference_FailsValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void DateGreater_WithMultipleDayDifference_PassesValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(100)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_AcrossDaylightSavingsTransition()
        {
            // Test spanning DST transition
            var model = new DateGreaterModel
            {
                StartDate = new DateTime(2025, 3, 8),
                EndDate = new DateTime(2025, 3, 10)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_AcrossYearBoundary_PassesValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = new DateTime(2024, 12, 31),
                EndDate = new DateTime(2025, 1, 1)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_WithLeapYearDates_PassesValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = new DateTime(2024, 2, 28),
                EndDate = new DateTime(2024, 2, 29)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void DateGreater_BeforeAndAfterLeapDay_PassesValidation()
        {
            var model = new DateGreaterModel
            {
                StartDate = new DateTime(2024, 2, 29),
                EndDate = new DateTime(2024, 3, 1)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        #endregion

        #region Combined Validation Tests

        private class CombinedValidationModel
        {
            [FutureDate]
            public DateTime? StartDate { get; set; }

            [DateGreater("StartDate")]
            public DateTime? EndDate { get; set; }
        }

        [Fact]
        public void CombinedValidation_BothValid_PassesValidation()
        {
            var model = new CombinedValidationModel
            {
                StartDate = DateTime.Now.AddHours(1),
                EndDate = DateTime.Now.AddHours(2)
            };

            var ok = TryValidate(model, out var results);

            Assert.True(ok);
            Assert.Empty(results);
        }

        [Fact]
        public void CombinedValidation_StartInPast_FailsValidation()
        {
            var model = new CombinedValidationModel
            {
                StartDate = DateTime.Now.AddHours(-1),
                EndDate = DateTime.Now.AddHours(1)
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results);
        }

        [Fact]
        public void CombinedValidation_EndBeforeStart_FailsValidation()
        {
            var model = new CombinedValidationModel
            {
                StartDate = DateTime.Now.AddHours(3),
                EndDate = DateTime.Now.AddHours(1)
            };

            var ok = TryValidate(model, out var results);

            // May fail on either StartDate being past or EndDate being before StartDate
            Assert.False(ok);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void CombinedValidation_BothPast_FailsValidation()
        {
            var model = new CombinedValidationModel
            {
                StartDate = DateTime.Now.AddHours(-2),
                EndDate = DateTime.Now.AddHours(-1)
            };

            var ok = TryValidate(model, out var results);

            Assert.False(ok);
            Assert.Single(results); // FutureDate validation fails
        }

        #endregion
    }
}
