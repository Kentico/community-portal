using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Features.Blog.Models;

public class BlogPostAuthorViewModel
{
    public string Name { get; set; } = "";
    public ImageAssetViewModel Avatar { get; set; }
    public string ProfileLinkPath { get; set; } = "";

    public BlogPostAuthorViewModel(AuthorContent author, ImageAssetViewModel avatar, string profileLinkPath)
    {
        Name = author.FullName;
        Avatar = avatar;
        ProfileLinkPath = profileLinkPath;
    }

    public BlogPostAuthorViewModel()
    {

    }
}
