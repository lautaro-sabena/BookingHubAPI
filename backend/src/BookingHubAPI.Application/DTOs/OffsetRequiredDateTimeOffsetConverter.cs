using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BookingHubAPI.Application.DTOs;

/// <summary>
/// Reads an ISO 8601 date-time that MUST carry a UTC offset ("Z" or "+hh:mm"). A value without one
/// names no instant (it would silently depend on a guessed time zone), so it is rejected and the
/// request fails model binding with a 400. Writing keeps the value's own offset.
/// </summary>
public sealed partial class OffsetRequiredDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public const string OffsetRequiredMessage =
        "Date-time must be ISO 8601 with a UTC offset, e.g. 2030-01-07T09:00:00-03:00 or 2030-01-07T12:00:00Z.";

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T.*(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetPattern();

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (text == null
            || !OffsetPattern().IsMatch(text)
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            throw new JsonException(OffsetRequiredMessage);
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
