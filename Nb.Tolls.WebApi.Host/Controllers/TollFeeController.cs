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
    private readonly ITollRequestValidator _tollRequestValidator;
    private readonly ITollFeesService _tollFeesService;
    private readonly ILogger<TollFeeController> _logger;

    public TollFeeController(
        ITollRequestValidator tollRequestValidator,
        ITollFeesService tollFeesService,
        ILogger<TollFeeController> logger) : base(logger)
    {
        _tollRequestValidator = tollRequestValidator;
        _tollFeesService = tollFeesService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TollFeesResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTollFees([FromBody] TollFeesRequest feesRequest)
    {
        try
        {
            _tollRequestValidator.ValidateTollTimes(feesRequest.TollTimes);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var applicationResult = await _tollFeesService.GetTollFees(feesRequest.VehicleType, feesRequest.TollTimes);
            if (!applicationResult.IsSuccessful)
            {
                return MapErrorResponse(applicationResult);
            }

            var result = applicationResult.Result;
            if (result == null)
            {
                _logger.LogError("TollTimeFeeResult was null despite applicationResultStatus being success");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            
            var tollFees = applicationResult.Result.TollFees;
            if (tollFees.Count == 0)
            {
                _logger.LogError("TollFees list was empty despite applicationResultStatus being success");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            
            var response = TollFeesMapper.Map(result);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollFees: {Message}", e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}