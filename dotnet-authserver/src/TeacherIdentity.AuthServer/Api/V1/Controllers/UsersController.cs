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
[Route("users")]
[Route("teachers")]
[Authorize(AuthorizationPolicies.GetAnIdentitySupportApi)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId}")]
    [SwaggerOperation(summary: "Get a user's details by their user ID")]
    [ProducesResponseType(typeof(GetUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [MapError(10003, statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetail([FromRoute] GetUserDetailRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(response);
    }

    [HttpGet("")]
    [SwaggerOperation(summary: "Retrieves all users")]
    [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        var response = await _mediator.Send(new GetAllUsersRequest());
        return Ok(response);
    }

    [HttpPut("{userId}/trn")]
    [SwaggerOperation(summary: "Set the TRN for a teacher")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [MapError(10003, statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTeacherTrn(
        [FromRoute(Name = "userId")] Guid userId,
        [FromBody] SetTeacherTrnRequestBody body)
    {
        var request = new SetTeacherTrnRequest()
        {
            UserId = userId,
            Body = body
        };
        await _mediator.Send(request);
        return NoContent();
    }
}
