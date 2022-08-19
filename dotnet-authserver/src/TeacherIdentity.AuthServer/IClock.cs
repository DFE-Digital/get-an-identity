namespace TeacherIdentity.AuthServer;

public interface IClock
{
    DateTime UtcNow { get; }
}
