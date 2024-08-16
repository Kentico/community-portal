using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.Filters;

namespace Kentico.Community.Portal.Admin.UIPages;

public class WhereLikeConditionBuilder : IWhereConditionBuilder
{
    public Task<IWhereCondition> Build(string columnName, object value)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            throw new ArgumentException(
                $"{nameof(columnName)} cannot be a null or an empty string.");
        }

        var whereCondition = new WhereCondition();

        if (value is null ||
            value is not string valueStr ||
            string.IsNullOrWhiteSpace(valueStr))
        {
            return Task.FromResult<IWhereCondition>(whereCondition);
        }

        _ = whereCondition.WhereLike(columnName, valueStr);

        return Task.FromResult<IWhereCondition>(whereCondition);
    }
}
