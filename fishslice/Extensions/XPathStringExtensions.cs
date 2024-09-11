namespace fishslice.Extensions;

public static class XPathStringExtensions
{
    public static string SanitiseXPath(this string rawXPath)
    {
        return rawXPath.StartsWith("//") ? rawXPath : $"xpath={rawXPath}";
    }

    public static string EscapeQuotesInXPath(this string rawXPath)
    {
        return rawXPath.Replace(@"""", @"\""");
    }
}