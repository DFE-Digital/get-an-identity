using MiniValidation;
using TeacherIdentity.AuthServer.Api.Endpoints;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Oidc.Endpoints;

public static class TrnLookupEndpoints
{
    public static IEndpointConventionBuilder MapTrnLookupEndpoints(this IEndpointRouteBuilder builder)
    {
        return builder.MapGroup("find-trn")
            .RequireAuthorization(AuthorizationPolicies.TrnLookupApi)
            .MapSetJourneyTrnLookupState();
    }

    private static IEndpointConventionBuilder MapSetJourneyTrnLookupState(this IEndpointRouteBuilder builder) =>
        builder
            .MapPut(
                "user/{journeyId}",
                async (Guid journeyId, SetJourneyTrnLookupStateRequestBody body, TeacherIdentityServerDbContext dbContext, IClock clock) =>
                {
                    if (!MiniValidator.TryValidate(body, out var errors))
                    {
                        return ErrorResult.Create(errors);
                    }

                    var existingState = await dbContext.JourneyTrnLookupStates.FindAsync(journeyId);

                    if (existingState?.Locked is not null)
                    {
                        return Results.BadRequest("Journey is locked.");
                    }

                    var normalizedNino = (body.NationalInsuranceNumber ?? string.Empty)
                        .ToUpper()
                        .Replace(" ", "");

                    if (existingState is null)
                    {
                        dbContext.JourneyTrnLookupStates.Add(new JourneyTrnLookupState()
                        {
                            Created = clock.UtcNow,
                            JourneyId = journeyId,
                            DateOfBirth = body.DateOfBirth,
                            OfficialFirstName = body.OfficialFirstName!,
                            OfficialLastName = body.OfficialLastName!,
                            Trn = body.Trn,
                            NationalInsuranceNumber = normalizedNino,
                            PreferredFirstName = body.PreferredFirstName!,
                            PreferredLastName = body.PreferredLastName!,
                            SupportTicketCreated = body.SupportTicketCreated
                        });
                    }
                    else
                    {
                        existingState.DateOfBirth = body.DateOfBirth;
                        existingState.OfficialFirstName = body.OfficialFirstName!;
                        existingState.OfficialLastName = body.OfficialLastName!;
                        existingState.Trn = body.Trn;
                        existingState.NationalInsuranceNumber = normalizedNino;
                        existingState.PreferredFirstName = body.PreferredFirstName!;
                        existingState.PreferredLastName = body.PreferredLastName!;
                    }

                    await dbContext.SaveChangesAsync();

                    return Results.NoContent();
                });
}
