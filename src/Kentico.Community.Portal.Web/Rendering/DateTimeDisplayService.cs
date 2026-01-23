namespace Kentico.Community.Portal.Web.Rendering;

/// <summary>
/// Provides timezone-aware datetime conversion for displaying dates/times to users
/// </summary>
public class DateTimeDisplayService
{
    /// <summary>
    /// Converts a UTC datetime to the user's timezone and returns display info
    /// </summary>
    /// <param name="utcDateTime">The UTC datetime to convert</param>
    /// <param name="timeZoneId">The user's timezone ID (e.g., "America/New_York"). If null/empty, returns UTC time.</param>
    /// <returns>Tuple of converted local datetime, timezone abbreviation, and full timezone label</returns>
    public (DateTime LocalDateTime, string Abbreviation, string Label) ConvertToUserTimezone(
        DateTime utcDateTime,
        string? timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId))
        {
            return (utcDateTime, "UTC", "UTC");
        }

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);

            // Get abbreviation based on daylight saving time status
            bool isDaylight = tz.IsDaylightSavingTime(localDateTime);
            string abbreviation = isDaylight ? tz.DaylightName : tz.StandardName;

            // Extract abbreviation from names like "Eastern Daylight Time" -> "EDT"
            string abbreviatedTz = ExtractAbbreviation(abbreviation);

            return (localDateTime, abbreviatedTz, tz.StandardName);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback to UTC if timezone is invalid
            return (utcDateTime, "UTC", "UTC");
        }
    }

    /// <summary>
    /// Extracts timezone abbreviation from full timezone name
    /// E.g., "Eastern Daylight Time" -> "EDT", "Central Standard Time" -> "CST"
    /// </summary>
    private string ExtractAbbreviation(string timezoneName)
    {
        string[] parts = timezoneName.Split(' ');
        if (parts.Length < 2)
        {
            return timezoneName.Substring(0, Math.Min(3, timezoneName.Length)).ToUpperInvariant();
        }

        // Take first letter of first two words (or more if needed) + last word initial
        string abbreviation = string.Empty;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            abbreviation += parts[i][0];
        }
        abbreviation += parts[^1][0]; // Last word initial

        return abbreviation.ToUpperInvariant();
    }
}
