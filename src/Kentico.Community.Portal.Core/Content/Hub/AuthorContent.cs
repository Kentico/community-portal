namespace Kentico.Community.Portal.Core.Content;

public partial class AuthorContent
{
    /// <summary>
    /// The code name of the shared "Community" author
    /// </summary>
    public const string KENTICO_AUTHOR_CODE_NAME = "kentico";

    public string FullName =>
        string.IsNullOrWhiteSpace(AuthorContentSurname)
            ? AuthorContentFirstName
            : $"{AuthorContentFirstName} {AuthorContentSurname}";

    public string ProfileRelativePath =>
        string.Equals(KENTICO_AUTHOR_CODE_NAME, AuthorContentCodeName) || AuthorContentMemberID == 0
            ? "#"
            : $"/member/{AuthorContentMemberID}";
}
