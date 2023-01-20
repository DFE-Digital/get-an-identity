namespace TeacherIdentity.AuthServer.Api;

public interface ICurrentUserProvider
{
    Guid CurrentUserId { get; }
}
