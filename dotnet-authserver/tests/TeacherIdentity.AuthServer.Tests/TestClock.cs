namespace TeacherIdentity.AuthServer.Tests;

public class TestClock : IClock
{
    public static DateTime Initial => new(2020, 4, 1, 11, 12, 13, DateTimeKind.Utc);  // Arbitrary point in time

    public DateTime UtcNow { get; private set; } = Initial;

    public DateTime AdvanceBy(TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpan));
        }

        UtcNow += timeSpan;
        return UtcNow;
    }
}
