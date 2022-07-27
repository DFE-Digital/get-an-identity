using TeacherIdentityServer.Models;

namespace TeacherIdentityServer;

public static class SessionExtensions
{
    private const string AuthenticateModelSessionKey = nameof(AuthenticateModel);

    public static AuthenticateModel GetAuthenticateModel(this ISession session)
    {
        var serialized = session.GetString(AuthenticateModelSessionKey);

        if (serialized == null)
        {
            return new AuthenticateModel();
        }

        return AuthenticateModel.Deserialize(serialized);
    }

    public static void UpdateAuthenticateModel(this ISession session, Action<AuthenticateModel> updateModel)
    {
        var model = GetAuthenticateModel(session);
        updateModel(model);
        session.SetString(AuthenticateModelSessionKey, model.Serialize());
    }
}
