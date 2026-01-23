namespace Kentico.Community.Portal.Core.Operations;

/// <summary>
/// Implementing type can generate cache dependency keys and cache item names for <see cref="IQuery{TResponse}"/>
/// </summary>
/// <typeparam name="TQuery"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IQueryCacheKeysCreator<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Creates cache dependency keys from the <paramref name="query"/> and <paramref name="response"/>
    /// </summary>
    /// <param name="query"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    public string[] DependencyKeys(TQuery query, TResponse response);

    /// <summary>
    /// Creates the name of the cached data from the <paramref name="query"/>
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public object[] ItemNameParts(TQuery query);
}
