namespace CMS.DataEngine;

public static class CommunityObjectQueryExtensions
{
    public static ObjectQuery<T> ApplyIf<T>(this ObjectQuery<T> sourceQuery, Action<ObjectQuery<T>> queryModification, bool condition)
        where T : BaseInfo
    {
        if (!condition)
        {
            return sourceQuery;
        }

        queryModification(sourceQuery);

        return sourceQuery;
    }

    public static ObjectQuery<T> ApplyIf<T>(this ObjectQuery<T> sourceQuery, Action<ObjectQuery<T>> queryModification, Func<bool> predicate)
        where T : BaseInfo
    {
        if (!predicate())
        {
            return sourceQuery;
        }

        queryModification(sourceQuery);

        return sourceQuery;
    }
}
