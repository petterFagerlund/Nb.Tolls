using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Services;
using Nb.Tolls.WebApi.Host.Mappers;
using Nb.Tolls.WebApi.Host.Validators;
using Nb.Tolls.WebApi.Models.Requests;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TollController : ApiControllerBase
{
    private readonly ITollRequestValidator _tollRequestValidator;
    private readonly ITollFeesService _tollFeesService;
    private readonly ILogger<TollController> _logger;

    public TollController(
        ITollRequestValidator tollRequestValidator,
        ITollFeesService tollFeesService,
        ILogger<TollController> logger) : base(logger)
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
    public async Task<IActionResult> GetTollFees([FromBody] TollRequest request)
    {
        try
        {
            _tollRequestValidator.ValidateTollTimes(request.TollTimes);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var applicationResult = await _tollFeesService.GetTollFees(request.VehicleType, request.TollTimes);
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