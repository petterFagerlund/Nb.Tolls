using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nb.Tolls.WebApi.Host.Validators.Implementation;

public class TollFeesFeesRequestValidator : ITollFeesRequestValidator
{
    public ModelStateDictionary ValidateTollTimes(DateTimeOffset[] tollTimes)
    {
        var modelState = new ModelStateDictionary();
        if (tollTimes.Length == 0)
        {
            modelState.AddModelError(nameof(tollTimes), "At least one toll time must be provided.");
            return modelState;
        }

        foreach (var tollTime in tollTimes)
        {
            if (tollTime > DateTimeOffset.UtcNow)
            {
                modelState.AddModelError(nameof(tollTimes), "must not be in the future.");
            }

            if (tollTime == DateTimeOffset.MaxValue)
            {
                modelState.AddModelError(nameof(tollTimes), "Must be a valid date.");
            }

            if (tollTime == DateTimeOffset.MinValue)
            {
                modelState.AddModelError(nameof(tollTimes), "Must be a valid date.");
            }
        }

        return modelState;
    }
}