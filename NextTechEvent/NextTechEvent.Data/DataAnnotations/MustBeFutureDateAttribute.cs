using System.ComponentModel.DataAnnotations;

namespace NextTechEvent.Data.DataAnnotations;

public class MustBeFutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateOnly)
        {
            var thedate = (DateOnly?)value;
            if (thedate == null)
                return new ValidationResult("You must select a date", new[] { validationContext.MemberName ?? "" });
            return thedate.Value >= DateOnly.FromDateTime(DateTime.Now.Date) ? ValidationResult.Success : new ValidationResult("The date must be today or a future date", new[] { validationContext.MemberName ?? "" });
        }
        else if (value is DateTime)
        {
            var thedate = (DateTime?)value;
            if (thedate == null)
                return new ValidationResult("You must select a date", new[] { validationContext.MemberName ?? "" });
            return thedate?.Date >= DateTime.Now.Date ? ValidationResult.Success : new ValidationResult("The date must be today or a future date", new[] { validationContext.MemberName ?? "" });
        }
        else
        {
            return new ValidationResult("Must be a date type", new[] { validationContext.MemberName ?? "" });
        }
    }
}
