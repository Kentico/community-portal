using CMS.Base;
using CMS.Core;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Tests.Fixtures;

/// <summary>
/// Turns read-only mode on (or off) until disposed.
/// </summary>
/// <remarks>
/// Xperience's <see cref="IReadOnlyModeProvider" /> reads <c>IOptions&lt;ReadOnlyModeOptions&gt;.Value.ReadOnly</c>
/// on every call and can't be substituted (it isn't publicly implementable), so the scope changes
/// the shared options instance. Every <see cref="IReadOnlyModeProvider" /> in the process sees it,
/// including the one Xperience's data layer uses to refuse writes - so create test data before
/// entering the scope. The E2E <c>POST /testing/e2e/read-only</c> endpoint uses the same approach.
/// </remarks>
public sealed class ReadOnlyModeScope : IDisposable
{
    private readonly bool previous;

    public ReadOnlyModeScope(bool readOnly = true)
    {
        previous = Options.ReadOnly;
        Options.ReadOnly = readOnly;
    }

    internal static ReadOnlyModeOptions Options => Service.Resolve<IOptions<ReadOnlyModeOptions>>().Value;

    public void Dispose() => Options.ReadOnly = previous;
}
