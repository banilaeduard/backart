using HtmlAgilityPack;
using System.Net;

namespace NER
{
    internal class HtmlStripper
    {
        internal static string StripHtml(string html)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var comments = htmlDoc.DocumentNode.SelectNodes("//comment()");
            if (comments != null)
                foreach (var node in comments)
                {
                    node.Remove();
                }
            return WebUtility.HtmlDecode(htmlDoc.DocumentNode.InnerText);
        }
    }
}