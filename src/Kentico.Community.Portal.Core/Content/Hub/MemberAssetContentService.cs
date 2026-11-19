using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Core.Content;

public class MemberAssetContentService(
    IContentRetriever contentRetriever)
{
    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<MemberAssetContent?> GetAvatarContentByMemberID(int memberID, CancellationToken token = default)
    {
        if (memberID <= 0)
        {
            return null;
        }

        var results = await contentRetriever.RetrieveContent<MemberAssetContent>(
            new RetrieveContentParameters
            {
                LanguageName = PortalWebSiteChannel.DEFAULT_LANGUAGE,
                IsForPreview = false,
            },
            query => query
                .TopN(1)
                .Where(where => where.WhereEquals(nameof(MemberAssetContent.MemberAssetContentMemberID), memberID)),
                new RetrievalCacheSettings($"member|{memberID}"),
                cancellationToken: token);

        return results.FirstOrDefault();
    }

    public async Task<string?> GetContentHubAvatarUrl(int memberID, CancellationToken token = default)
    {
        if (memberID <= 0)
        {
            return null;
        }

        var contentItem = await GetAvatarContentByMemberID(memberID, token);
        if (contentItem?.MemberAssetContentImageAsset is not null)
        {
            return contentItem.MemberAssetContentImageAsset.Url;
        }

        return null;
    }

    public Task<string?> GetAvatarAssetUrl(int memberID, CancellationToken token = default) =>
        GetContentHubAvatarUrl(memberID, token);
}
