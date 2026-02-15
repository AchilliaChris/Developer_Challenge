using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace DeveloperChallenge.Validators
{
    public class GuestCapacityAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public GuestCapacityAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is ICollection == false) { return new ValidationResult(ErrorMessage = "Guest required for a room"); }
            var currentValue = ((ICollection)value).Count;

            var comparisonValue = (int)validationContext.ObjectType.GetProperty(_comparisonProperty)
                                                                        .GetValue(validationContext.ObjectInstance);

            if (currentValue > comparisonValue)
            {
                return new ValidationResult(ErrorMessage = "Guest number cannot exceed room capacity");
            }

            return ValidationResult.Success;
        }
    }
}
