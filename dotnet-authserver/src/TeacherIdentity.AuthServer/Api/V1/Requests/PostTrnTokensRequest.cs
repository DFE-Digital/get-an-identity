using System.ComponentModel.DataAnnotations;
using MediatR;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record PostTrnTokensRequest : IRequest<PostTrnTokenResponse>
{
    [RegularExpression(@"^\d{7}$", ErrorMessage = "TRN must be 7 digits")]
    public required string Trn { get; init; }

    [EmailAddress]
    public required string Email { get; init; }
}
