using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Xunit.Sdk;

namespace TeacherIdentity.AuthServer.Tests;

public static class HttpResponseMessageExtensions
{
    public static async Task<IHtmlDocument> GetDocument(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        var doc = (IHtmlDocument)await browsingContext.OpenAsync(req => req.Content(content));

        AssertSmartQuotesUsed();

        return doc;

        void AssertSmartQuotesUsed()
        {
            VisitDocumentNodes(
                doc,
                node =>
                {
                    if (node.NodeType != NodeType.Text)
                    {
                        return;
                    }

                    if (node.ParentElement is IHtmlScriptElement)
                    {
                        return;
                    }

                    using var reader = new StringReader(node.Text());
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var nonSmartQuoteIndex = line.IndexOf('\'');
                        if (nonSmartQuoteIndex != -1)
                        {
                            var indicatorLine = new string(' ', nonSmartQuoteIndex) + "^";
                            var message = $"Missing smart quote:\n{line}\n{indicatorLine}";
                            throw new XunitException(message);
                        }
                    }
                });
        }

        void VisitDocumentNodes(IHtmlDocument document, Action<INode> visit)
        {
            VisitNode(document.DocumentElement);

            void VisitNode(INode node)
            {
                visit(node);

                foreach (var child in node.GetDescendants())
                {
                    visit(child);
                }
            }
        }
    }
}
