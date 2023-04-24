namespace TeacherIdentity.AuthServer.Models;

[Flags]
public enum WebHookMessageTypes
{
    None = 0,

    UserUpdated = 1 << 0,
    UserMerged = 1 << 1,
    UserCreated = 1 << 2,

    All = UserUpdated | UserMerged | UserCreated
}
