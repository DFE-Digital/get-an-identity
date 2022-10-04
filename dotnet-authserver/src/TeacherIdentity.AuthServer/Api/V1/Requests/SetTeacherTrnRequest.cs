using MediatR;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public class SetTeacherTrnRequest : IRequest
{
    public Guid UserId { get; set; }
    public SetTeacherTrnRequestBody? Body { get; set; }
}

public class SetTeacherTrnRequestBody
{
    public string? Trn { get; set; }
}
