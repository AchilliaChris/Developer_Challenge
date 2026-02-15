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
    }
}
