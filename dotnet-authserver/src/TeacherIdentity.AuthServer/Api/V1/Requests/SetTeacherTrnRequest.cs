using MediatR;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record SetTeacherTrnRequest : IRequest
{
    public required Guid UserId { get; init; }
    public required SetTeacherTrnRequestBody Body { get; init; }
}

public record SetTeacherTrnRequestBody
{
    public required string Trn { get; init; }
}
