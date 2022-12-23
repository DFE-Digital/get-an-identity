using MediatR;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record GetAllUsersRequest : IRequest<GetAllUsersResponse>
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    [ModelBinder(typeof(CommaSeparatedModelBinder))]
    public TrnLookupStatus[]? TrnLookupStatus { get; set; }
}
