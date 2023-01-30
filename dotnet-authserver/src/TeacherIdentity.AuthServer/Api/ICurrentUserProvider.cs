namespace TeacherIdentity.AuthServer.Api;

public interface ICurrentUserProvider
{
    string CurrentClientId { get; }
    Guid? CurrentUserId { get; }
}
