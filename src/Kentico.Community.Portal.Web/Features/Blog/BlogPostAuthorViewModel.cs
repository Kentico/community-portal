namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostAuthorViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public MediaAssetContent? Avatar { get; set; }

    public BlogPostAuthorViewModel(AuthorContent author, MediaAssetContent? avatar)
    {
        Name = author.FullName;
        Avatar = avatar;
        ID = author.AuthorContentMemberID;
    }

    public BlogPostAuthorViewModel()
    {

    }
}
