using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Features.Blog.Models;

public class BlogPostAuthorViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public ImageAssetViewModel Avatar { get; set; }

    public BlogPostAuthorViewModel(AuthorContent author, ImageAssetViewModel avatar)
    {
        Name = author.FullName;
        Avatar = avatar;
        ID = author.AuthorContentMemberID;
    }

    public BlogPostAuthorViewModel()
    {

    }
}
