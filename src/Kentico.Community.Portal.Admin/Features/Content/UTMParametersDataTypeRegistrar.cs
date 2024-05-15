using System.Data;
using System.Globalization;
using CMS.DataEngine;
using CMS.DataEngine.Internal;
using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Content;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Newtonsoft.Json;

[assembly: RegisterFormComponent(
    UTMParametersFormComponent.IDENTIFIER,
    typeof(UTMParametersFormComponent),
    "UTM Parameters")]

namespace Kentico.Community.Portal.Admin;

public static class UTMParametersDataTypeRegistrar
{
    public static void Register(IDataTypeRegister register)
    {
        DataTypeRegistration utm = new(
            new DataType<UTMParametersDataType>(
                sqlType: "nvarchar(max)",
                fieldType: UTMParametersDataType.FIELD_TYPE,
                schemaType: "xs:string",
                conversionFunc: JsonDataTypeConverter.ConvertToModel,
                dbConversionFunc: JsonDataTypeConverter.ConvertToString,
                textSerializer: new DefaultDataTypeTextSerializer(UTMParametersDataType.FIELD_TYPE))
            {
                TypeAlias = "string",
                TypeGroup = "String",
                SqlValueFormat = DataTypeManager.UNICODE,
                DbType = SqlDbType.NVarChar,
                DefaultValueCode = JsonConvert.SerializeObject(new UTMParametersDataType()),
                DefaultValue = new(),
                HasConfigurableDefaultValue = true,
                IsAvailableForDataClass = (c) => true
            },
            new(UTMParametersDataType.FIELD_TYPE,
                () => new DataTypeCodeGenerator(
                    field => "UTMParametersDataType",
                    field => nameof(ValidationHelper.GetString),
                    field => "new()",
                    field => ["Kentico.Community.Portal.Core.Content"])),
            new(UTMParametersDataType.FIELD_TYPE,
                UTMParametersFormComponent.IDENTIFIER,
                (val) => val is UTMParametersDataType utmDt ? JsonDataTypeConverter.ConvertToString(utmDt, new(), CultureInfo.CurrentCulture).ToString() ?? "" : "",
                (val) => JsonDataTypeConverter.ConvertToModel<UTMParametersDataType>(val, new(), CultureInfo.CurrentCulture))
        );

        register.AddRegistrations([utm]);
    }
}

public class UTMParametersFormComponentAttribute : FormComponentAttribute { }

[ComponentAttribute(typeof(UTMParametersFormComponentAttribute))]
public class UTMParametersFormComponent : FormComponent<
    UTMParametersFormComponentProperties,
    UTMParametersFormComponentClientProperties,
    UTMParametersDataType>
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Admin.FormComponent.UTMParameters";

    public override string ClientComponentName => "@kentico-community/portal-web-admin/UTMParametersDataType";

    protected async override Task ConfigureClientProperties(UTMParametersFormComponentClientProperties clientProperties)
    {
        await base.ConfigureClientProperties(clientProperties);

        clientProperties.VisibleFields = UTMParameterFieldsDataProvider.Items
            .Where(i => Properties.VisibleFields.Contains(i.Value))
            .Select(i => new UTMParametersVisibleField(i.Text, i.Value));
    }
}

public class UTMParametersFormComponentProperties : FormComponentProperties
{
    [GeneralSelectorComponent(
        typeof(UTMParameterFieldsDataProvider),
        Label = "Visible fields",
        Placeholder = "None",
        Order = 0)]
    public IEnumerable<string> VisibleFields { get; set; } = UTMParameterFieldsDataProvider.Items.Select(i => i.Value);
}

public class UTMParametersFormComponentClientProperties : FormComponentClientProperties<UTMParametersDataType>
{
    public UTMParametersDataType NewUTMParameters { get; } = new UTMParametersDataType();
    public IEnumerable<UTMParametersVisibleField> VisibleFields { get; set; } = [];
}

public record UTMParametersVisibleField(string Text, string Value);

public class UTMParameterFieldsDataProvider : IGeneralSelectorDataProvider
{
    private static ObjectSelectorListItem<string> Source { get; } = new()
    {
        Value = "source",
        Text = "Source",
        IsValid = true
    };
    private static ObjectSelectorListItem<string> Medium { get; } = new()
    {
        Value = "medium",
        Text = "Medium",
        IsValid = true
    };
    private static ObjectSelectorListItem<string> Campaign { get; } = new()
    {
        Value = "campaign",
        Text = "Campaign",
        IsValid = true
    };
    private static ObjectSelectorListItem<string> Content { get; } = new()
    {
        Value = "content",
        Text = "Content",
        IsValid = true
    };
    private static ObjectSelectorListItem<string> Term { get; } = new()
    {
        Value = "term",
        Text = "Term",
        IsValid = true
    };
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };
    public static readonly IEnumerable<ObjectSelectorListItem<string>> Items = [Source, Medium, Campaign, Content, Term];

    public Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var searchItems = string.IsNullOrEmpty(searchTerm)
            ? Items
            : Items.Where(i => i.Text.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = searchItems,
        });
    }

    public Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken) =>
        Task.FromResult(selectedValues?.Select(GetSelectedItemByValue) ?? []);

    private static ObjectSelectorListItem<string> GetSelectedItemByValue(string contentTypeTypeValue) =>
        contentTypeTypeValue switch
        {
            "source" => Source,
            "medium" => Medium,
            "campaign" => Campaign,
            "content" => Content,
            "term" => Term,
            _ => InvalidItem
        };
}
