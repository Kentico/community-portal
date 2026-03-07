namespace Kentico.Community.Portal.Core.Content;

public partial class AuthorContent
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("8033c6de-9e47-4618-ac1a-25fd361c6ac8");

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
