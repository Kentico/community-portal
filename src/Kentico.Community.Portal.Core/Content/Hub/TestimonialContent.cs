namespace Kentico.Community.Portal.Core.Content;

public partial class TestimonialContent
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("338dd1e2-33c7-45d4-be87-80827ad5b288");

    public TestimonialContentDataSources TestimonialContentDataSourceParsed =>
        Enum.TryParse<TestimonialContentDataSources>(TestimonialContentDataSource, out var result)
            ? result
            : TestimonialContentDataSources.Custom;
}

public enum TestimonialContentDataSources
{
    Member,
    Custom
}
