namespace TeacherIdentity.AuthServer;

public static class EnumerableExtensions
{
    public static bool SequenceEqualIgnoringOrder<T>(this IEnumerable<T> first, IEnumerable<T> second)
        where T : IComparable
    {
        var firstArray = first.ToArray().OrderBy(s => s);
        var secondArray = second.ToArray().OrderBy(s => s);
        return firstArray.SequenceEqual(secondArray);
    }
}
