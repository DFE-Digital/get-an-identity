using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record UpdateUserRequest : IRequest<UserInfo>
{
    public required Guid UserId { get; set; }
    public required UpdateUserRequestBody Body { get; set; }
}

public record UpdateUserRequestBody
{
    private string? _email;
    private string? _firstName;
    private string? _lastName;

    [SwaggerSchema(Nullable = false)]
    public string? Email
    {
        get => _email;
        set
        {
            _email = value;
            EmailSet = true;
        }
    }

    [SwaggerSchema(Nullable = false)]
    public string? FirstName
    {
        get => _firstName;
        set
        {
            _firstName = value;
            FirstNameSet = true;
        }
    }

    [SwaggerSchema(Nullable = false)]
    public string? LastName
    {
        get => _lastName;
        set
        {
            _lastName = value;
            LastNameSet = true;
        }
    }

    internal bool EmailSet { get; private set; } = false;

    internal bool FirstNameSet { get; private set; } = false;

    internal bool LastNameSet { get; private set; } = false;
}
