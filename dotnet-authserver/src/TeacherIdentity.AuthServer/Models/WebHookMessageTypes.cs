namespace TeacherIdentity.AuthServer.Models;

[Flags]
public enum WebHookMessageTypes
{
    None = 0,

    UserUpdated = 1 << 0,
    UserMerged = 1 << 1,

    All = UserUpdated | UserMerged
}
