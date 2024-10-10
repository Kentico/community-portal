namespace CMS.ContentEngine;

public static class CommunityContentItemQueryExtensions
{
    public static WhereParameters WhereContentItem(this WhereParameters w, Guid contentItemGUID) =>
        w.WhereEquals(nameof(ContentItemFields.ContentItemGUID), contentItemGUID);

    public static WhereParameters WhereContentItem(this WhereParameters w, int contentItemID) =>
        w.WhereEquals(nameof(ContentItemFields.ContentItemID), contentItemID);
}
