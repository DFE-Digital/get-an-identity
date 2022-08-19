namespace TeacherIdentity.AuthServer;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
