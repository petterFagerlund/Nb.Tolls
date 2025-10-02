using Microsoft.AspNetCore.Mvc;
using Nb.Tolls.Application.Services;
using Nb.Tolls.WebApi.Host.Mappers;
using Nb.Tolls.WebApi.Host.Requests;
using Nb.Tolls.WebApi.Host.Responses;
using Nb.Tolls.WebApi.Host.Validators;

namespace Nb.Tolls.WebApi.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TollFeeController : ApiControllerBase
{
    private readonly ITollFeesRequestValidator _tollFeesRequestValidator;
    private readonly ITollFeesCalculatorService _tollFeesCalculatorService;
    private readonly ILogger<TollFeeController> _logger;

    public TollFeeController(
        ITollFeesRequestValidator tollFeesRequestValidator,
        ITollFeesCalculatorService tollFeesCalculatorService,
        ILogger<TollFeeController> logger) : base(logger)
    {
        _tollFeesRequestValidator = tollFeesRequestValidator;
        _tollFeesCalculatorService = tollFeesCalculatorService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TollFeeResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTollFees([FromBody] TollFeesRequest tollFeesRequest)
    {
        _tollFeesRequestValidator.ValidateTollTimes(tollFeesRequest.TollTimes);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var applicationResult = await _tollFeesCalculatorService.CalculateTollFees(tollFeesRequest.VehicleType, tollFeesRequest.TollTimes);
        if (!applicationResult.IsSuccessful)
        {
            return MapErrorResponse(applicationResult);
        }

        var result = applicationResult.Result!;
        if (result?.Count == 0)
        {
            _logger.LogInformation("No toll fees applicable for the provided data.");
            return NotFound("No toll fees applicable for the provided data.");
        }

        var response = TollFeesResponseMapper.Map(result);
        return Ok(response);
    }
}