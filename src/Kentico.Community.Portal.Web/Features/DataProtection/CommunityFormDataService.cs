using CMS.DataEngine;
using CMS.FormEngine;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.DataProtection;
using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// A Form Builder form with where it stores personal identifiers and its retention policy.
/// </summary>
/// <param name="Policy">Null when the form is unclassified (kept, with a warning from the retention task).</param>
public record CommunityForm(BizFormInfo Form, string ClassName, FormDataShape Shape, FormRetentionPolicyInfo? Policy)
{
    public bool IsUnclassified => Policy is null;

    public bool IsShortLived =>
        string.Equals(Policy?.FormRetentionPolicyType, FormRetentionPolicyTypes.SHORT_LIVED, StringComparison.OrdinalIgnoreCase);

    public int ShortLivedRetentionDays(RetentionOptions options) =>
        Policy is { FormRetentionPolicyRetentionDays: > 0 } policy
            ? policy.FormRetentionPolicyRetentionDays
            : options.ShortLivedFormDefaultDays;
}

/// <summary>
/// Finds, scrubs and deletes form submissions by the data subject's email and member IDs. Forms are
/// discovered from their field definitions (<see cref="FormDataShape"/>), not a list of names, so
/// production-only forms are covered.
/// </summary>
public class CommunityFormDataService(
    IInfoProvider<BizFormInfo> formProvider,
    IInfoProvider<FormRetentionPolicyInfo> policyProvider)
{
    private const string CONTACT_EMAIL_MAPPING_FIELD = "ContactEmail";
    private const string CONTACT_MAPPING_COLUMN = "ClassContactMapping";
    private const string COMPONENT_IDENTIFIER_SETTING = "componentidentifier";

    private readonly IInfoProvider<BizFormInfo> formProvider = formProvider;
    private readonly IInfoProvider<FormRetentionPolicyInfo> policyProvider = policyProvider;

    public IReadOnlyList<CommunityForm> GetForms()
    {
        var policies = policyProvider.Get()
            .AsEnumerable()
            .GroupBy(p => p.FormRetentionPolicyFormID)
            .ToDictionary(g => g.Key, g => g.First());

        var forms = new List<CommunityForm>();

        foreach (var form in formProvider.Get().OrderBy(nameof(BizFormInfo.FormDisplayName)))
        {
            var dataClass = DataClassInfoProvider.GetDataClassInfo(form.FormClassID);
            if (dataClass is null)
            {
                continue;
            }

            forms.Add(new CommunityForm(
                form,
                dataClass.ClassName,
                DetectShape(dataClass),
                policies.GetValueOrDefault(form.FormID)));
        }

        return forms;
    }

    public static FormDataShape DetectShape(DataClassInfo dataClass)
    {
        var formInfo = new FormInfo(dataClass.ClassFormDefinition);
        var fields = formInfo
            .GetFields(visible: true, invisible: true, includeSystem: false, onlyPrimaryKeys: false, includeDummyFields: false)
            .Select(f => (f.Name, f.Settings[COMPONENT_IDENTIFIER_SETTING] as string));

        return FormDataShape.Detect(fields, GetContactEmailMappedField(dataClass));
    }

    private static string? GetContactEmailMappedField(DataClassInfo dataClass)
    {
        string mapping = dataClass.GetStringValue(CONTACT_MAPPING_COLUMN, "");
        if (string.IsNullOrWhiteSpace(mapping))
        {
            return null;
        }

        var field = new FormInfo(mapping).GetFormField(CONTACT_EMAIL_MAPPING_FIELD);

        return string.IsNullOrWhiteSpace(field?.MappedToField) ? null : field.MappedToField;
    }

    /// <summary>
    /// Rows that hold one of the emails in any email column, or one of the member IDs.
    /// </summary>
    public IReadOnlyList<BizFormItem> FindItems(CommunityForm form, IReadOnlyCollection<string> emails, IReadOnlyCollection<int> memberIDs)
    {
        var where = Matching(form.Shape, emails, memberIDs);
        if (where is null)
        {
            return [];
        }

        return [.. BizFormItemProvider.GetItems(form.ClassName).Where(where)];
    }

    /// <summary>Replaces the data subject's email with <paramref name="placeholder"/>. The row and member ID stay.</summary>
    public int ScrubEmails(CommunityForm form, IReadOnlyCollection<string> emails, IReadOnlyCollection<int> memberIDs, string placeholder)
    {
        int count = 0;

        foreach (var item in FindItems(form, emails, memberIDs))
        {
            bool changed = false;

            foreach (string column in form.Shape.AllEmailColumns)
            {
                string value = item.GetStringValue(column, "");
                bool isSubjectsEmail = emails.Contains(value, StringComparer.OrdinalIgnoreCase);
                bool isMembersRow = form.Shape.MemberIDColumn is string idColumn
                    && memberIDs.Contains(item.GetIntegerValue(idColumn, 0))
                    && string.Equals(column, form.Shape.MemberEmailColumn, StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(value) && (isSubjectsEmail || isMembersRow))
                {
                    item.SetValue(column, placeholder);
                    changed = true;
                }
            }

            if (changed)
            {
                item.Update();
                count++;
            }
        }

        return count;
    }

    public int DeleteItems(CommunityForm form, IReadOnlyCollection<string> emails, IReadOnlyCollection<int> memberIDs)
    {
        var items = FindItems(form, emails, memberIDs);

        foreach (var item in items)
        {
            item.Delete();
        }

        return items.Count;
    }

    /// <summary>Deletes submissions inserted before <paramref name="cutoff"/>.</summary>
    public int DeleteItemsInsertedBefore(CommunityForm form, DateTime cutoff)
    {
        var items = BizFormItemProvider.GetItems(form.ClassName)
            .WhereLessThan("FormInserted", cutoff)
            .ToList();

        foreach (var item in items)
        {
            item.Delete();
        }

        return items.Count;
    }

    private static WhereCondition? Matching(FormDataShape shape, IReadOnlyCollection<string> emails, IReadOnlyCollection<int> memberIDs)
    {
        var where = new WhereCondition();
        bool any = false;

        if (emails.Count > 0)
        {
            foreach (string column in shape.AllEmailColumns)
            {
                _ = where.Or().WhereIn(column, emails.ToList());
                any = true;
            }
        }

        if (memberIDs.Count > 0 && shape.MemberIDColumn is string idColumn)
        {
            _ = where.Or().WhereIn(idColumn, memberIDs.ToList());
            any = true;
        }

        return any ? where : null;
    }
}
