using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace TeacherIdentity.AuthServer.Tests;

public static class AngleSharpExtensions
{
    public static T? As<T>(this IElement element)
        where T : class, IElement
    {
        return element as T;
    }

    public static IReadOnlyList<IElement> GetAllElementsByTestId(this IElement element, string testId) =>
        element.QuerySelectorAll($"*[data-testid='{testId}']").ToList();

    public static IElement? GetElementByTestId(this IElement element, string testId) =>
        GetAllElementsByTestId(element, testId).SingleOrDefault();

    public static IReadOnlyList<IElement> GetAllElementsByTestId(this IHtmlDocument doc, string testId) =>
        doc.Body!.GetAllElementsByTestId(testId);

    public static IElement? GetElementByLabel(this IHtmlDocument doc, string label)
    {
        var allLabels = doc.QuerySelectorAll("label");

        foreach (var l in allLabels)
        {
            if (l.TextContent.Trim() == label)
            {
                var @for = l.GetAttribute("for");
                return @for is not null ? doc.GetElementById(@for) : null;
            }
        }

        return null;
    }

    public static IElement? GetElementByTestId(this IHtmlDocument doc, string testId) =>
        doc.Body!.GetElementByTestId(testId);

    public static IReadOnlyList<IElement> GetSummaryListActionsForKey(this IHtmlDocument doc, string key)
    {
        var row = GetSummaryListRowForKey(doc, key);
        return row?.QuerySelectorAll(".govuk-summary-list__actions>*").ToArray() ?? Array.Empty<IElement>();
    }

    public static int GetSummaryListRowCountForKey(this IHtmlDocument doc, string key)
    {
        var count = 0;
        var allRows = doc.QuerySelectorAll(".govuk-summary-list__row");

        foreach (var row in allRows)
        {
            var rowKey = row.QuerySelector(".govuk-summary-list__key");

            if (rowKey?.TextContent.Trim() == key)
            {
                count++;
            }
        }

        return count;
    }

    public static IElement? GetSummaryListRowForKey(this IHtmlDocument doc, string key)
    {
        var allRows = doc.QuerySelectorAll(".govuk-summary-list__row");

        foreach (var row in allRows)
        {
            var rowKey = row.QuerySelector(".govuk-summary-list__key");

            if (rowKey?.TextContent.Trim() == key)
            {
                return row;
            }
        }

        return null;
    }

    public static string? GetSummaryListValueForKey(this IHtmlDocument doc, string key)
    {
        var row = GetSummaryListRowForKey(doc, key);
        var rowValue = row?.QuerySelector(".govuk-summary-list__value");
        return rowValue?.TextContent.Trim();
    }
}
