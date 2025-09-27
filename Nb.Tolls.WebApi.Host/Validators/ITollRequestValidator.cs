using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nb.Tolls.WebApi.Host.Validators;

public interface ITollRequestValidator
{
    ModelStateDictionary ValidateTollTimes(DateTimeOffset[] request);
}