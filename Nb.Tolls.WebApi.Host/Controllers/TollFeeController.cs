using Microsoft.AspNetCore.Mvc;
using Nb.Tolls.Application.Services;
using Nb.Tolls.WebApi.Host.Mappers;
using Nb.Tolls.WebApi.Host.Validators;
using Nb.Tolls.WebApi.Models.Requests;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TollFeeController : ApiControllerBase
{
    private readonly ITollFeesRequestValidator _tollFeesRequestValidator;
    private readonly ITollFeesService _tollFeesService;
    private readonly ILogger<TollFeeController> _logger;

    public TollFeeController(
        ITollFeesRequestValidator tollFeesRequestValidator,
        ITollFeesService tollFeesService,
        ILogger<TollFeeController> logger) : base(logger)
    {
        _tollFeesRequestValidator = tollFeesRequestValidator;
        _tollFeesService = tollFeesService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TollFeesResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTollFees([FromBody] TollFeesRequest tollFeesRequest)
    {
        try
        {
            _tollFeesRequestValidator.ValidateTollTimes(tollFeesRequest.TollTimes);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var applicationResult = await _tollFeesService.GetTollFees(tollFeesRequest.VehicleType, tollFeesRequest.TollTimes);
            if (!applicationResult.IsSuccessful)
            {
                return MapErrorResponse(applicationResult);
            }

            var result = applicationResult.Result;
            if (result == null)
            {
                _logger.LogError("TollFeeResult was null despite applicationResultStatus being success");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            
            var response = TollFeesResponseMapper.Map(result);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollFees: {Message}", e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}