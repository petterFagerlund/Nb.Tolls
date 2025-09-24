using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Application.Services;
using Nb.Tolls.WebApi.Host.Mappers;
using Nb.Tolls.WebApi.Models.Requests;
using Nb.Tolls.WebApi.Models.Responses;

namespace Nb.Tolls.WebApi.Host.Controllers;

public class TollsController : ApiControllerBase
{
    private readonly ITollService _tollService;
    private readonly ILogger<TollsController> _logger;

    public TollsController(ITollService tollService, ILogger<TollsController> logger) : base(logger)
    {
        _tollService = tollService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TollResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTolls([FromBody] TollRequest request)
    {
        try
        {
            //Todo: Define and validate request parameters

            var applicationResult = await _tollService.GetTollAsync();
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
            _logger.LogError("Exception in GetTolls: {Message}", e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}