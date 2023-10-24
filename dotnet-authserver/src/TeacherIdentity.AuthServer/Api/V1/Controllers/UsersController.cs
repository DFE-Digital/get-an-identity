using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TeacherIdentity.AuthServer.Api.Filters;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Api.V1.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(AuthorizationPolicies.ApiUserRead)]
    [HttpGet("{UserId}")]
    [SwaggerOperation(summary: "Get a user's details by their user ID")]
    [ProducesResponseType(typeof(GetUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [MapError(10003, statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetail([FromRoute] GetUserDetailRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(response);
    }

    [Authorize(AuthorizationPolicies.ApiUserRead)]
    [HttpGet("")]
    [SwaggerOperation(summary: "Retrieves all users")]
    [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersRequest request)
    {
        var response = await _mediator.Send(request);
        return Ok(response);
    }

    [Authorize(AuthorizationPolicies.ApiUserWrite)]
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

    [Authorize(AuthorizationPolicies.ApiUserWrite)]
    [HttpPatch("{userId}")]
    [SwaggerOperation(summary: "Updates a user")]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [MapError(10003, statusCode: StatusCodes.Status404NotFound)]
    [MapError(10005, statusCode: StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute(Name = "userId")] Guid userId,
        [FromBody] UpdateUserRequestBody? body)
    {
        var request = new UpdateUserRequest()
        {
            UserId = userId,
            Body = body ?? new()
        };
        var response = await _mediator.Send(request);
        return Ok(response);
    }
}
