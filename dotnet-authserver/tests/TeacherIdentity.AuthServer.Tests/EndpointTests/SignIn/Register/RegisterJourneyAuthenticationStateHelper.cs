using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public delegate AuthenticationStateConfiguration AuthenticationStateConfigGenerator(
    User? user = null,
    string? mobileNumber = null,
    bool? awardedQts = null);

public static class RegisterJourneyAuthenticationStateHelper
{
    public static AuthenticationStateConfigGenerator ConfigureAuthenticationStateForPage(RegisterJourneyPage page)
    {
        return (user, mobileNumber, awardedQts) =>
        {
            switch (page)
            {
                case RegisterJourneyPage.Index:
                case RegisterJourneyPage.Email:
                    return c => c.Start();

                case RegisterJourneyPage.EmailConfirmation:
                case RegisterJourneyPage.ResendEmail:
                    return c => c.EmailSet();

                case RegisterJourneyPage.Phone:
                    return c => c.EmailVerified();

                case RegisterJourneyPage.InstitutionEmail:
                    return c => c.EmailVerified(isInstitutionEmail: true);

                case RegisterJourneyPage.PhoneConfirmation:
                case RegisterJourneyPage.ResendPhone:
                    return c => c.MobileNumberSet(mobileNumber);

                case RegisterJourneyPage.Name:
                    return c => c.MobileVerified();

                case RegisterJourneyPage.PreferredName:
                    return c => c.RegisterNameSet();

                case RegisterJourneyPage.DateOfBirth:
                    return c => c.RegisterPreferredNameSet();

                case RegisterJourneyPage.HasNiNumber:
                    return c => c.RegisterDateOfBirthSet();

                case RegisterJourneyPage.NiNumber:
                    return c => c.RegisterHasNiNumberSet();

                case RegisterJourneyPage.HasTrn:
                    return c => c.RegisterNiNumberSet();

                case RegisterJourneyPage.Trn:
                    return c => c.RegisterHasTrnSet();

                case RegisterJourneyPage.HasQts:
                    return c => c.RegisterTrnSet();

                case RegisterJourneyPage.IttProvider:
                    return c => c.RegisterHasQtsSet(awardedQts: awardedQts);

                case RegisterJourneyPage.CheckAnswers:
                    return c => c.RegisterIttProviderSet(awardedQts: awardedQts, mobileNumber: mobileNumber);

                case RegisterJourneyPage.AccountExists:
                    return c => c.RegisterExistingUserAccountMatch(user);

                case RegisterJourneyPage.ExistingAccountEmailConfirmation:
                case RegisterJourneyPage.ResendExistingAccountEmail:
                case RegisterJourneyPage.ExistingAccountPhone:
                case RegisterJourneyPage.ExistingAccountPhoneConfirmation:
                case RegisterJourneyPage.ResendExistingAccountPhone:
                    return c => c.RegisterExistingUserAccountChosen(user);

                case RegisterJourneyPage.EmailExists:
                case RegisterJourneyPage.PhoneExists:
                    return c => c.Completed(user!);


                default:
                    return c => c.Start();
            }
        };
    }
}

public enum RegisterJourneyPage
{
    Index,
    Email,
    EmailConfirmation,
    InstitutionEmail,
    EmailExists,
    ResendEmail,
    Phone,
    PhoneConfirmation,
    PhoneExists,
    ResendPhone,
    Name,
    PreferredName,
    DateOfBirth,
    AccountExists,
    ExistingAccountEmailConfirmation,
    ResendExistingAccountEmail,
    ExistingAccountPhone,
    ExistingAccountPhoneConfirmation,
    ResendExistingAccountPhone,
    HasNiNumber,
    NiNumber,
    HasTrn,
    Trn,
    HasQts,
    IttProvider,
    CheckAnswers,
}
