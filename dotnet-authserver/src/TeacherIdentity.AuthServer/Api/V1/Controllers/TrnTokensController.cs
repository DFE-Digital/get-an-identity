using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TeacherIdentity.AuthServer.Api.Filters;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Infrastructure.Security;

namespace TeacherIdentity.AuthServer.Api.V1.Controllers;

[ApiController]
[Route("trn-tokens")]
public class TrnTokensController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrnTokensController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(AuthorizationPolicies.ApiTrnTokenWrite)]
    [HttpPost("")]
    [SwaggerOperation(summary: "Generates a TRN token")]
    [ProducesResponseType(typeof(CreateTrnTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [MapError(10003, statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTrnToken([FromBody] CreateTrnTokenRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(response);
    }
}
