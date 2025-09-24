using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nb.Tolls.WebApi.Models.Requests;

namespace Nb.Tolls.WebApi.Host.Validators.Implementation;

public class TollRequestValidator : ITollRequestValidator
{
    public ModelStateDictionary ValidateTollTimes(TollRequest request)
    {
        var modelState = new ModelStateDictionary();
        foreach (var tollTime in request.TollTimes)
        {
            if (tollTime > DateTimeOffset.UtcNow)
            {
                modelState.AddModelError(nameof(request.TollTimes), "must not be in the future.");
            }
            
            if (tollTime == DateTimeOffset.MaxValue)
            {
                modelState.AddModelError(nameof(request.TollTimes), "Must be a valid date.");
            }
            
            if (tollTime == DateTimeOffset.MinValue)
            {
                modelState.AddModelError(nameof(request.TollTimes), "Must be a valid date.");
            }
        }
        return modelState;
    }
}