using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;

namespace Nb.Tolls.WebApi.Host.Controllers;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string[]))]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
[Authorize]
#if DEBUG
[AllowAnonymous]
#endif
public class ApiControllerBase : ControllerBase
{
    private readonly ILogger<ApiControllerBase> _logger;

    public ApiControllerBase(ILogger<ApiControllerBase> logger)
    {
        _logger = logger;
    }

    internal IActionResult MapErrorResponse(ApplicationResult applicationResult)
    {
        switch (applicationResult.ApplicationResultStatus)
        {
            case ApplicationResultStatus.NotFound:
                return NotFound();
            case ApplicationResultStatus.Forbidden:
                return Forbid();
            case ApplicationResultStatus.ValidationError:
                return ValidationProblem(
                    new ValidationProblemDetails
                    {
                        Errors = new Dictionary<string, string[]> { { "request", applicationResult.Messages } }
                    });
            case ApplicationResultStatus.Conflict:
                return Conflict(applicationResult.Messages);
            case ApplicationResultStatus.Error:
                return StatusCode(StatusCodes.Status500InternalServerError, applicationResult.Messages);
            default:
            {
                _logger.LogError(
                    "ApplicationResultStatus code not mapped. Status: {ApplicationResultStatus}",
                    applicationResult);
                return StatusCode(StatusCodes.Status500InternalServerError, "ApplicationResultStatus not mapped");
            }
        }
    }
}