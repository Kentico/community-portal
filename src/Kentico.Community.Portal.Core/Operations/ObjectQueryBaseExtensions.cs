namespace CMS.DataEngine;

public static class ObjectQueryBaseExtensions
{
    /// <summary>
    /// Materializes and executes the query, returning a single item from the result set. If there are zero or more than one item in the result set, an exception is thrown.
    /// To return the first item among several, use <see cref="FirstOrDefaultAsync" />
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <typeparam name="TObject"></typeparam>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException" />
    /// <returns></returns>
    public static async Task<TObject> SingleAsync<TQuery, TObject>(this ObjectQueryBase<TQuery, TObject> query, CancellationToken? cancellationToken = null)
        where TQuery : ObjectQueryBase<TQuery, TObject>, new() where TObject : BaseInfo
    {
        var result = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return result.Single();
    }

    /// <summary>
    /// Materializes and executes the query, returning the first item from the result set, or null if there are no items.
    /// To enforce only a single item being returned, use <see cref="SingleAsync"/> 
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <typeparam name="TObject"></typeparam>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<TObject?> FirstOrDefaultAsync<TQuery, TObject>(this ObjectQueryBase<TQuery, TObject> query, CancellationToken? cancellationToken = null)
        where TQuery : ObjectQueryBase<TQuery, TObject>, new() where TObject : BaseInfo
    {
        var result = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return result.FirstOrDefault();
    }
}
