using CMS.Core;
using CMS.DataEngine;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;
using MediatR;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Vogen;

namespace Kentico.Community.Portal.Web.Components.Widgets.Forms;

public static class FormState
{
    public const string FORM_IS_HIDDEN = nameof(FORM_IS_HIDDEN);

    public static bool GetIsFormHidden<T>(ViewDataDictionary<T> viewData) =>
        viewData.TryGetValue(FORM_IS_HIDDEN, out object? isHiddenObj) && isHiddenObj is bool isHidden && isHidden;

    /// <summary>
    /// Stores the state of the <see cref="IHideableComponent"/> in <see cref="ViewDataDictionary{TModel}"/>
    /// for child widgets and form components
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="viewData"></param>
    /// <param name="props"></param>
    public static void SetIsFormHidden<T>(ViewDataDictionary<T> viewData, IHideableComponent props) =>
        viewData.Add(FORM_IS_HIDDEN, props.IsHidden);
}

public static class FormParser
{
    public static Maybe<BizFormInfo> GetFormByClassName(FormClassName formClassName) =>
        Maybe<DataClassInfo>.From(DataClassInfoProvider.GetDataClassInfo(formClassName.Value))
            .Map(dc => BizFormInfoProvider.GetBizFormInfoForClass(dc.ClassID));

    public static Dictionary<string, string> GetFormFieldOptions(BizFormInfo form, string fieldName) =>
        ParseFieldDataSource(form, fieldName);

    public static PollFormData GetPollFormData(BizFormInfo form)
    {
        var options = ParseFieldDataSource(form, "Question");
        string? label = form.Form.GetFormField("Question").Caption;

        return new(label ?? "", options);
    }

    private static Dictionary<string, string> ParseFieldDataSource(BizFormInfo form, string fieldName)
    {
        object? dataSource = form.Form.GetFormField(fieldName).Settings["DataSource"];

        if (dataSource is not string srcString || string.IsNullOrWhiteSpace(srcString))
        {
            return [];
        }

        var lookup = new Dictionary<string, string>();
        string[] lines = srcString.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string[] parts = line.Split(';');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                lookup[key] = value;
            }
            else if (parts.Length == 1)
            {
                string key = parts[0];
                lookup[key] = key;
            }
        }

        return lookup;
    }
}

public record PollFormData(string Label, Dictionary<string, string> Options);

public interface IHideableComponent
{
    public bool IsHidden { get; }
}

public interface IFormMemberEngagementRetriever
{
    public Task<bool> HasCurrentMemberSubmittedForm(Maybe<string> formCodeName);
}

public class FormMemberEngagementRetriever(
    IHttpContextAccessor contextAccessor,
    IMediator mediator) : IFormMemberEngagementRetriever
{
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IMediator mediator = mediator;

    public async Task<bool> HasCurrentMemberSubmittedForm(Maybe<string> formCodeName)
    {
        if (!formCodeName.TryGetValue(out string? codeName))
        {
            return false;
        }

        var memberID = CommunityMember.GetMemberIDFromClaim(contextAccessor.HttpContext);
        if (memberID == CommunityMemberID.Anonymous)
        {
            return false;
        }

        var resp = await mediator.Send(new FormByFormNameQuery(codeName));
        if (!resp.TryGetValue(out var r))
        {
            return false;
        }

        return await mediator.Send(new MemberFormEngagementQuery(memberID, r.FormClassName));
    }
}

/// <summary>
/// 
/// </summary>
/// <param name="MemberID"></param>
/// <param name="FormCodeName">Must be a form with a "MemberID" field</param>
public record MemberFormEngagementQuery(CommunityMemberID MemberID, FormClassName FormClassName) : IQuery<bool>, ICacheByValueQuery
{
    public string CacheValueKey => $"Form_Class:{FormClassName}|Member:{MemberID}";
}

public class MemberFormEngagementQueryHandler(DataItemQueryTools tools) : DataItemQueryHandler<MemberFormEngagementQuery, bool>(tools)
{
    public override async Task<bool> Handle(MemberFormEngagementQuery request, CancellationToken cancellationToken = default)
    {
        int foundMemberID = await BizFormItemProvider.GetItems(request.FormClassName.Value)
            // All Poll forms require a "MemberID" field
            .WhereEquals("MemberID", request.MemberID.Value)
            .Column("MemberID")
            .GetScalarResultAsync(0);

        return foundMemberID > 0;
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MemberFormEngagementQuery query, bool result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(query.FormClassName.Value);
}


public record FormByFormNameQuery(string FormCodeName) : IQuery<Maybe<FormByFormNameQueryResponse>>, ICacheByValueQuery
{
    public string CacheValueKey => FormCodeName;
}
public record FormByFormNameQueryResponse(BizFormInfo Form, FormClassName FormClassName);

public class FormByFormNameQueryHandler(DataItemQueryTools tools, IConversionService conversion) : DataItemQueryHandler<FormByFormNameQuery, Maybe<FormByFormNameQueryResponse>>(tools)
{
    private readonly IConversionService conversion = conversion;

    public override async Task<Maybe<FormByFormNameQueryResponse>> Handle(FormByFormNameQuery request, CancellationToken cancellationToken = default)
    {
        var results = await BizFormInfoProvider.ProviderObject
            .Get()
            .Source(s => s.Join<DataClassInfo>(nameof(BizFormInfo.FormClassID), nameof(DataClassInfo.ClassID)))
            .WhereEquals(nameof(BizFormInfo.FormName), request.FormCodeName)
            .AddColumn(new QueryColumn(nameof(DataClassInfo.ClassName)).As("DataClassName"))
            .GetDataContainerResultAsync();

        return results
            .TryFirst()
            .Map(r =>
            {
                var form = BizFormInfo.New(r);
                string name = conversion.GetString(r.GetValue("DataClassName"), "");
                var className = FormClassName.From(name);
                return new FormByFormNameQueryResponse(form, className);
            });
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(FormByFormNameQuery query, Maybe<FormByFormNameQueryResponse> result, ICacheDependencyKeysBuilder builder) =>
        builder.Object(BizFormInfo.OBJECT_TYPE, query.FormCodeName);
}

[ValueObject<string>]
public readonly partial struct FormClassName;
