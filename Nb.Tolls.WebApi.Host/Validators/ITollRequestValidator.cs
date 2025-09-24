using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nb.Tolls.WebApi.Models.Requests;

namespace Nb.Tolls.WebApi.Host.Validators;

public interface ITollRequestValidator
{
    ModelStateDictionary ValidateTollTimes(TollRequest request);
}