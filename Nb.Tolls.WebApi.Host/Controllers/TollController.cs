using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Services;
using Nb.Tolls.WebApi.Host.Mappers;
using Nb.Tolls.WebApi.Host.Validators;
using Nb.Tolls.WebApi.Host.Validators.Implementation;
using Nb.Tolls.WebApi.Models.Requests;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Controllers;

public class TollController : ApiControllerBase
{
    private readonly ITollRequestValidator _tollRequestValidator;
    private readonly ITollService _tollService;
    private readonly ILogger<TollController> _logger;

    public TollController(
        ITollRequestValidator tollRequestValidator,
        ITollService tollService,
        ILogger<TollController> logger) : base(logger)
    {
        _tollRequestValidator = tollRequestValidator;
        _tollService = tollService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TollResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTollFee([FromBody] TollRequest request)
    {
        try
        {
            _tollRequestValidator.ValidateTollTimes(request);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var applicationResult = await _tollService.GetTollFeeAsync(request.VehicleType, request.TollTimes);
            if (!applicationResult.IsSuccessful)
            {
                return MapErrorResponse(applicationResult);
            }

            var result = applicationResult.Result;
            if (result == null)
            {
                _logger.LogError("TollResult was null despite applicationResultStatus being success");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var response = TollMapper.Map(result);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in GetTollFee: {Message}", e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}