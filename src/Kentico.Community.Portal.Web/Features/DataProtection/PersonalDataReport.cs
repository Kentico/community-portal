using System.Globalization;
using System.Text;
using System.Text.Json;
using CMS.DataProtection;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// One kind of personal data (member, Q&amp;A answers, a form's submissions, …) and its rows.
/// </summary>
public record PersonalDataSection(string Title, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

/// <summary>
/// The collected personal data, written as text (Right to access, Right to be forgotten) or JSON
/// (Data portability).
/// </summary>
public class PersonalDataReport
{
    private readonly List<PersonalDataSection> sections = [];

    public IReadOnlyList<PersonalDataSection> Sections => sections;

    public void Add(string title, IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        var list = rows.ToList();
        if (list.Count > 0)
        {
            sections.Add(new PersonalDataSection(title, list));
        }
    }

    public string Write(string outputFormat) =>
        string.Equals(outputFormat, PersonalDataFormat.MACHINE_READABLE, StringComparison.OrdinalIgnoreCase)
            ? ToJson()
            : ToText();

    public string ToText()
    {
        var builder = new StringBuilder();

        foreach (var section in sections)
        {
            _ = builder.AppendLine($"{section.Title}:");

            foreach (var row in section.Rows)
            {
                foreach (var (key, value) in row)
                {
                    if (Format(value) is string text && text.Length > 0)
                    {
                        _ = builder.AppendLine($"\t{key}: {text}");
                    }
                }

                _ = builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    public string ToJson()
    {
        var root = sections
            .GroupBy(s => s.Title)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(s => s.Rows).Select(r => r.ToDictionary(kv => kv.Key, kv => Format(kv.Value))).ToList());

        return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string? Format(object? value) => value switch
    {
        null or DBNull => null,
        DateTime date => date.ToString("u", CultureInfo.InvariantCulture),
        bool flag => flag ? "Yes" : "No",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}

/// <summary>
/// The portal's single data collector returns one JSON object for Data portability. Xperience
/// wraps machine-readable output in an XML root by default; this keeps it valid JSON.
/// </summary>
public class JsonPersonalDataHelper : PersonalDataHelper
{
    protected override string JoinPersonalDataInternal(IEnumerable<string> personalData, string outputFormat)
    {
        if (!string.Equals(outputFormat, PersonalDataFormat.MACHINE_READABLE, StringComparison.OrdinalIgnoreCase))
        {
            return base.JoinPersonalDataInternal(personalData, outputFormat);
        }

        var parts = personalData.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

        return parts.Count switch
        {
            0 => "{}",
            1 => parts[0],
            _ => $"[{string.Join(",", parts)}]",
        };
    }
}
