using Microsoft.Net.Http.Headers;

namespace HelpDesk.Api.Http;

public static class IfMatchParser
{
    public static IfMatchParseResult Parse(HttpRequest request)
    {
        if (!request.Headers.ContainsKey(HeaderNames.IfMatch))
        {
            return new(IfMatchParseStatus.Missing, null);
        }

        var rawValue = request.Headers.IfMatch.ToString();
        if (!EntityTagHeaderValue.TryParse(rawValue, out var etag))
        {
            return new(IfMatchParseStatus.Invalid, null);
        }

        if (etag.IsWeak)
        {
            return new(IfMatchParseStatus.Invalid, null);
        }

        var base64 = etag.Tag.Value.Trim('"');
        byte[] version;
        try
        {
            version = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return new(IfMatchParseStatus.Invalid, null);
        }

        if (version.Length != 8)
        {
            return new(IfMatchParseStatus.Invalid, null);
        }

        return new(IfMatchParseStatus.Success, version);
    }
}
public enum IfMatchParseStatus
{
    Success,
    Missing,
    Invalid
}

public record IfMatchParseResult(
    IfMatchParseStatus Status,
    byte[]? Version);