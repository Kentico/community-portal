using System.Data;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Content;

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <param name="ContentTypeName">Code name of the content type.</param>
/// <param name="DisplayName">The item's display name in its first language.</param>
public record MemberContentItem(string ContentTypeName, int ContentItemID, string ContentItemName, string DisplayName, string LanguageName);

/// <summary>
/// Content items (pages and reusable items) that point at a member through a plain integer member
/// ID field. These fields have no foreign key, so nothing else finds them.
/// </summary>
public class MemberContentItemsQuery
{
    /// <summary>Content type → its member ID field.</summary>
    public static IReadOnlyDictionary<string, string> MemberFields { get; } = new Dictionary<string, string>
    {
        [QAndAQuestionPage.CONTENT_TYPE_NAME] = nameof(QAndAQuestionPage.QAndAQuestionPageAuthorMemberID),
        [MemberProfileContent.CONTENT_TYPE_NAME] = nameof(MemberProfileContent.MemberProfileContentMemberID),
        [MemberAssetContent.CONTENT_TYPE_NAME] = nameof(MemberAssetContent.MemberAssetContentMemberID),
        [AuthorContent.CONTENT_TYPE_NAME] = nameof(AuthorContent.AuthorContentMemberID),
        [TestimonialContent.CONTENT_TYPE_NAME] = nameof(TestimonialContent.TestimonialContentMemberID),
        [IntegrationContent.CONTENT_TYPE_NAME] = nameof(IntegrationContent.IntegrationContentAuthorMemberID),
        [LinkContent.CONTENT_TYPE_NAME] = nameof(LinkContent.LinkContentMemberID),
    };

    public IReadOnlyList<MemberContentItem> Get(IReadOnlyCollection<int> memberIDs, params string[] contentTypeNames)
    {
        if (memberIDs.Count == 0)
        {
            return [];
        }

        var types = contentTypeNames.Length > 0 ? contentTypeNames : [.. MemberFields.Keys];
        var items = new List<MemberContentItem>();
        string ids = string.Join(",", memberIDs.Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        foreach (string type in types)
        {
            if (!MemberFields.TryGetValue(type, out string? column)
                || DataClassInfoProvider.GetDataClassInfo(type) is not DataClassInfo dataClass
                || string.IsNullOrEmpty(dataClass.ClassTableName))
            {
                continue;
            }

            // Table and column names come from the class definitions above, never from input
            string sql = $"""
                SELECT DISTINCT i.ContentItemID, i.ContentItemName, m.ContentItemLanguageMetadataDisplayName AS DisplayName, l.ContentLanguageName AS LanguageName
                FROM [{dataClass.ClassTableName}] t
                INNER JOIN CMS_ContentItemCommonData c ON c.ContentItemCommonDataID = t.ContentItemDataCommonDataID
                INNER JOIN CMS_ContentItem i ON i.ContentItemID = c.ContentItemCommonDataContentItemID
                INNER JOIN CMS_ContentLanguage l ON l.ContentLanguageID = c.ContentItemCommonDataContentLanguageID
                LEFT JOIN CMS_ContentItemLanguageMetadata m
                    ON m.ContentItemLanguageMetadataContentItemID = i.ContentItemID
                    AND m.ContentItemLanguageMetadataContentLanguageID = c.ContentItemCommonDataContentLanguageID
                WHERE t.[{column}] IN ({ids})
                """;

            var data = ConnectionHelper.ExecuteQuery(sql, null, QueryTypeEnum.SQLQuery);
            if (data.Tables.Count == 0)
            {
                continue;
            }

            foreach (DataRow row in data.Tables[0].Rows)
            {
                items.Add(new MemberContentItem(
                    type,
                    (int)row["ContentItemID"],
                    row["ContentItemName"] as string ?? "",
                    row["DisplayName"] as string ?? "",
                    row["LanguageName"] as string ?? ""));
            }
        }

        return items;
    }
}
