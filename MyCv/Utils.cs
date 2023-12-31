using System.Text;
using System.Text.RegularExpressions;

namespace MyCv;

internal partial class Utils
{
    [GeneratedRegex(@"<a\shref=""mailto:(\w*)@(\w*)\.(\w*)"">[^<\/>]*<\/a>", RegexOptions.Compiled, "de")]
    private static partial Regex MailToRegex();

    [GeneratedRegex("\\s", RegexOptions.Compiled, "de")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"[^a-z0-9\s-_]", RegexOptions.Compiled, "de")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"([-_]){2,}", RegexOptions.Compiled, "de")]
    private static partial Regex DoubleStrokeRegex();

    public static string ToUrlSlug(string value)
    {
        //First to lower case
        value = value.ToLowerInvariant();

        //Remove all accents
        // var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(value);
        //  value = Encoding.ASCII.GetString(bytes);

        //Replace spaces
        value = WhiteSpaceRegex().Replace(value, "-");

        //Remove invalid chars
        value = InvalidCharsRegex().Replace(value, "");

        //Trim dashes from end
        value = value.Trim('-', '_');

        //Replace double occurences of - or _
        value = DoubleStrokeRegex().Replace(value, "$1");

        return value;
    }

    public static string MailProtection(string html) =>
        MailToRegex().Replace(html, @"$1_AT_$2_DOT_$3");
    
}

public static class Extensions
{
    public static string UrlSlug(this OtherSide otherSide)
        => Utils.ToUrlSlug(otherSide.Title);
}