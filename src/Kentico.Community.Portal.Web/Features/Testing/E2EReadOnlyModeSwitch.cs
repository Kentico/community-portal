using CMS.Base;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// Turns read-only mode on or off in the running application.
/// </summary>
/// <remarks>
/// <para>
/// Xperience's <see cref="IReadOnlyModeProvider" /> reads <c>IOptions&lt;ReadOnlyModeOptions&gt;.Value.ReadOnly</c>
/// on every call, and <see cref="IOptions{TOptions}" /> caches one options instance for the app's
/// lifetime, so changing that instance switches the whole process. The interface itself can't be
/// replaced: it is not publicly implementable.
/// </para>
/// <para>
/// Process-wide: every concurrent request and spec sees the change, and Xperience refuses database
/// writes while it is on. Use it only from a serial spec, and turn it off in a <c>finally</c>.
/// An app restart resets it to the configured value.
/// </para>
/// </remarks>
public class E2EReadOnlyModeSwitch(IOptions<ReadOnlyModeOptions> options, IReadOnlyModeProvider readOnlyModeProvider)
{
    private readonly IOptions<ReadOnlyModeOptions> options = options;
    private readonly IReadOnlyModeProvider readOnlyModeProvider = readOnlyModeProvider;

    public E2EReadOnlyModeResponse SetEnabled(bool enabled)
    {
        options.Value.ReadOnly = enabled;

        return new E2EReadOnlyModeResponse(readOnlyModeProvider.IsReadOnly);
    }

    public E2EReadOnlyModeResponse Get() => new(readOnlyModeProvider.IsReadOnly);
}

public record E2EReadOnlyModeRequest(bool Enabled);

public record E2EReadOnlyModeResponse(bool Enabled);
