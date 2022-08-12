namespace TeacherIdentity.AuthServer.Tests;

public class TestClock : IClock
{
    public DateTime UtcNow => new(2020, 4, 1, 11, 12, 13, DateTimeKind.Utc);  // Arbitrary point in time
}
