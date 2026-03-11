using System.ComponentModel.DataAnnotations;

namespace DeveloperChallenge.Validators
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            
        }

        // Validate the date comparison
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = (DateTime)value;

            var comparisonValue = DateTime.Now;

            if (currentValue < comparisonValue)
            {
                return new ValidationResult(ErrorMessage = "Start date must be in the future");
            }

            return ValidationResult.Success;
        }
    }
}
