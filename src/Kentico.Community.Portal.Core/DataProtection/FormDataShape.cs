namespace Kentico.Community.Portal.Core.DataProtection;

/// <summary>
/// Where a form stores personal identifiers, found from its field definitions rather than its name,
/// so production-only forms are covered too.
/// </summary>
/// <param name="MemberIDColumn">The member ID column, if any.</param>
/// <param name="MemberEmailColumn">The member email column, if any.</param>
/// <param name="EmailColumns">Other columns holding an email address (email inputs, the contact email mapping).</param>
public record FormDataShape(string? MemberIDColumn, string? MemberEmailColumn, IReadOnlyList<string> EmailColumns)
{
    public const string MEMBER_ID_COLUMN = "MemberID";
    public const string MEMBER_EMAIL_COLUMN = "MemberEmail";
    public const string MEMBER_ID_COMPONENT = "CommunityPortal.FormComponent.HiddenMemberID";
    public const string MEMBER_EMAIL_COMPONENT = "CommunityPortal.FormComponent.HiddenMemberEmail";
    public const string EMAIL_COMPONENT = "Kentico.EmailInput";

    /// <summary>True when the form holds anything that identifies a person.</summary>
    public bool HasPersonalIdentifiers =>
        MemberIDColumn is not null || MemberEmailColumn is not null || EmailColumns.Count > 0;

    /// <summary>Every column that can hold the data subject's email.</summary>
    public IReadOnlyList<string> AllEmailColumns =>
        [.. (MemberEmailColumn is null ? EmailColumns : EmailColumns.Prepend(MemberEmailColumn))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <param name="fields">Column name and Form Builder component identifier of each field.</param>
    /// <param name="contactEmailMappedField">The form field mapped to the contact's email, if any.</param>
    public static FormDataShape Detect(IEnumerable<(string Name, string? ComponentIdentifier)> fields, string? contactEmailMappedField)
    {
        string? memberIDColumn = null;
        string? memberEmailColumn = null;
        var emailColumns = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, component) in fields)
        {
            _ = names.Add(name);

            if (Is(component, MEMBER_ID_COMPONENT) || Is(name, MEMBER_ID_COLUMN))
            {
                memberIDColumn ??= name;
            }
            else if (Is(component, MEMBER_EMAIL_COMPONENT) || Is(name, MEMBER_EMAIL_COLUMN))
            {
                memberEmailColumn ??= name;
            }
            else if (Is(component, EMAIL_COMPONENT))
            {
                emailColumns.Add(name);
            }
        }

        if (!string.IsNullOrWhiteSpace(contactEmailMappedField)
            && names.TryGetValue(contactEmailMappedField, out string? mapped)
            && !Is(mapped, memberEmailColumn)
            && !emailColumns.Contains(mapped, StringComparer.OrdinalIgnoreCase))
        {
            emailColumns.Add(mapped);
        }

        return new FormDataShape(memberIDColumn, memberEmailColumn, emailColumns);
    }

    private static bool Is(string? value, string? expected) =>
        value is not null && expected is not null && string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
