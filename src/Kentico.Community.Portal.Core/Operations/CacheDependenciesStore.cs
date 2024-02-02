using System.Collections.Concurrent;

namespace Kentico.Community.Portal.Core.Operations;

public interface ICacheDependenciesStore
{
    /// <summary>
    /// Stores the cache dependency keys in the current scope
    /// </summary>
    /// <param name="keys"></param>
    void Store(string[] keys);
    /// <summary>
    /// Updates the internal state of the current scope in the store marking caching as not enabled for that scope.
    /// </summary>
    /// <remarks>
    /// When an operation within a scope fails we don't want to cache
    /// the output since this output will be missing the content from the failed operations.
    /// </remarks>
    void MarkCacheDisabled();
}

public interface ICacheDependenciesScope
{
    /// <summary>
    /// Creates a new cache dependency scope, which defines a new set for storing
    /// any cache dependency keys created within this scope
    /// </summary>
    void Begin();

    /// <summary>
    /// Ends the current scope (if it exists) and returns the cache dependency keys
    /// created while it was active. This also assigns any keys from the current scope
    /// to its parent.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> End();

    /// <summary>
    /// Defaults to true. Changed to false if any failed operations occur.
    /// </summary>
    bool IsCacheEnabled { get; }
}

/// <summary>
/// Represents a set of cache dependency keys for a scoped set of operations
/// which can be in a success or failure state
/// </summary>
/// <remarks>
/// We use a record type so we can modify its properties without needing to
/// remove it from the stack and then re-add it
/// </remarks>
internal record CacheScope
{
    public static CacheScope Success(HashSet<string> keys) => new(keys);
    public static CacheScope Failure() => new();

    private readonly object keysLock = new();
    private readonly HashSet<string> keysInternal;

    public bool HasFailure { get; set; }

    public void Add(string key)
    {
        lock (keysLock)
        {
            _ = keysInternal.Add(key);
        }
    }

    public IEnumerable<string> Keys => keysInternal.AsEnumerable();

    protected CacheScope()
    {
        HasFailure = true;
        keysInternal = [];
    }

    protected CacheScope(HashSet<string> keys)
    {
        HasFailure = false;
        keysInternal = keys;
    }
}

public class CacheDependenciesStore : ICacheDependenciesStore, ICacheDependenciesScope
{
    private readonly ConcurrentStack<CacheScope> cacheScopes = new();

    public bool IsCacheEnabled
    {
        get
        {
            if (!cacheScopes.TryPeek(out var currentScope))
            {
                return true;
            }

            return !currentScope.HasFailure;
        }
    }

    public void Begin() => cacheScopes.Push(CacheScope.Success([]));

    public void Store(string[] keys)
    {
        if (keys.Length == 0)
        {
            return;
        }

        if (!cacheScopes.TryPeek(out var currentScope))
        {
            return;
        }

        foreach (string key in keys)
        {
            currentScope.Add(key);
        }
    }

    public IEnumerable<string> End()
    {
        if (!cacheScopes.TryPop(out var currentScope))
        {
            return Enumerable.Empty<string>();
        }

        if (!cacheScopes.TryPeek(out var parentScope))
        {
            return currentScope.Keys.AsEnumerable();
        }

        foreach (string key in currentScope.Keys)
        {
            parentScope.Add(key);
        }

        if (currentScope.HasFailure)
        {
            parentScope.HasFailure = true;
        }

        return currentScope.Keys.AsEnumerable();
    }

    public void MarkCacheDisabled()
    {
        if (!cacheScopes.TryPeek(out var currentScope))
        {
            return;
        }

        currentScope.HasFailure = true;
    }
}
