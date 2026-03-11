using System.ComponentModel.DataAnnotations;

namespace DeveloperChallenge.Validators
{
    public class DateGreaterAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        // Set the name of the property to compare
        public DateGreaterAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        // Validate the date comparison
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = (DateTime)value;

            var comparisonValue = (DateTime)validationContext.ObjectType.GetProperty(_comparisonProperty)
                                                                        .GetValue(validationContext.ObjectInstance);

            if (currentValue < comparisonValue)
            {
                return new ValidationResult(ErrorMessage = "End date must be later than or equal to start date");
            }

            return ValidationResult.Success;
        }
    }
}
