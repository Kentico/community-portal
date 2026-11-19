using System.Collections.Concurrent;
using CMS.ContactManagement;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;

namespace Kentico.Community.Portal.Web.Features.Testing;

/// <summary>
/// Lets a Playwright browser context ask "which contacts did my requests create?" instead of
/// counting contacts globally, which parallel specs make flaky. The browser sends
/// <see cref="HEADER_NAME" /> on its requests and <see cref="E2EContactCorrelationHandler" /> records
/// every contact inserted during a request that carries it.
/// </summary>
/// <remarks>
/// Only registered when <see cref="E2ETestingGate.IsEnabled" />. Every contact creation path the portal has is
/// in-request (<c>IContactCreator.CreateAnonymousContact</c> from GetCurrentContact, form
/// submission, member sign-in and the subscription confirmation controller, plus the portal's own
/// <c>IInfoProvider&lt;ContactInfo&gt;.Set</c> calls). Xperience's background workers only queue
/// recalculations; none insert contacts. A contact inserted without an HttpContext would not be
/// recorded, so the handler logs it to make that gap visible.
/// </remarks>
public static class E2EContactCorrelation
{
    public const string HEADER_NAME = "x-e2e-correlation";

    public static IServiceCollection AddE2EContactCorrelation(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        if (!E2ETestingGate.IsEnabled(config, env))
        {
            return services;
        }

        return services
            .AddHttpContextAccessor()
            .AddSingleton<E2EContactCorrelationStore>()
            .AddInfoObjectEventHandler<InfoObjectAfterInsertEvent<ContactInfo>, E2EContactCorrelationHandler>();
    }
}

/// <summary>
/// Bounded and expiring, so a long-running Development app can't grow it without limit. The
/// limits are far above what one test run uses; an evicted correlation would read as "no
/// contacts", so keep them generous.
/// </summary>
public sealed class E2EContactCorrelationStore : IDisposable
{
    private const int MAX_CORRELATIONS = 10_000;
    private const int MAX_CONTACTS_PER_CORRELATION = 100;
    public const int MAX_CORRELATION_LENGTH = 64;
    private static readonly TimeSpan expiry = TimeSpan.FromHours(2);

    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = MAX_CORRELATIONS });

    public void Record(string correlation, E2ECorrelatedContact contact)
    {
        var contacts = cache.GetOrCreate(correlation, entry =>
        {
            entry.Size = 1;
            entry.AbsoluteExpirationRelativeToNow = expiry;

            return new ConcurrentQueue<E2ECorrelatedContact>();
        });

        if (contacts is not null && contacts.Count < MAX_CONTACTS_PER_CORRELATION)
        {
            contacts.Enqueue(contact);
        }
    }

    public IReadOnlyList<E2ECorrelatedContact> Get(string correlation) =>
        cache.TryGetValue<ConcurrentQueue<E2ECorrelatedContact>>(correlation, out var contacts) && contacts is not null
            ? [.. contacts]
            : [];

    public void Dispose() => cache.Dispose();
}

public class E2EContactCorrelationHandler(
    IHttpContextAccessor httpContextAccessor,
    E2EContactCorrelationStore store,
    ILogger<E2EContactCorrelationHandler> logger) :
        IInfoObjectEventHandler<InfoObjectAfterInsertEvent<ContactInfo>>
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly E2EContactCorrelationStore store = store;
    private readonly ILogger<E2EContactCorrelationHandler> logger = logger;

    public void Handle(InfoObjectAfterInsertEvent<ContactInfo> infoObjectEvent) =>
        Record(infoObjectEvent.InfoObject);

    public Task HandleAsync(InfoObjectAfterInsertEvent<ContactInfo> infoObjectEvent, CancellationToken cancellationToken)
    {
        Record(infoObjectEvent.InfoObject);

        return Task.CompletedTask;
    }

    private void Record(ContactInfo contact)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            logger.LogInformation("E2E: contact {ContactGUID} inserted outside a request, so it can't be correlated", contact.ContactGUID);

            return;
        }

        string correlation = httpContext.Request.Headers[E2EContactCorrelation.HEADER_NAME].ToString();

        if (string.IsNullOrEmpty(correlation) || correlation.Length > E2EContactCorrelationStore.MAX_CORRELATION_LENGTH)
        {
            return;
        }

        store.Record(correlation, new E2ECorrelatedContact(
            contact.ContactGUID,
            contact.ContactCreated,
            string.IsNullOrEmpty(contact.ContactEmail) ? null : contact.ContactEmail));
    }
}

/// <summary>Values at insert time. The contact may since have been merged or deleted.</summary>
public record E2ECorrelatedContact(Guid ContactGUID, DateTime Created, string? Email);

public record E2ECorrelatedContactsResponse(IReadOnlyList<E2ECorrelatedContact> Contacts);
