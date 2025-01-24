namespace Kentico.Community.Portal.Core.Content;

public interface IPortalPage
{
    public string Title { get; }
    public string ShortDescription { get; }
}

public record UnmanagedPage(string Title, string ShortDescription) : IPortalPage;
