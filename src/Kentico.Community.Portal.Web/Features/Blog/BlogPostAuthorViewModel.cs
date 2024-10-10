using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Features.Blog;

public class BlogPostAuthorViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public Maybe<ImageViewModel> Photo { get; set; }

    public BlogPostAuthorViewModel(AuthorContent author)
    {
        Name = author.FullName;
        Photo = author.ToImageViewModel();
        ID = author.AuthorContentMemberID;
    }

    public BlogPostAuthorViewModel()
    {

    }
}
