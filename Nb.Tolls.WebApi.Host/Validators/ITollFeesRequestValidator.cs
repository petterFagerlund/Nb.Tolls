using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nb.Tolls.WebApi.Host.Validators;

public interface ITollFeesRequestValidator
{
    ModelStateDictionary ValidateTollTimes(DateTimeOffset[] request);
}