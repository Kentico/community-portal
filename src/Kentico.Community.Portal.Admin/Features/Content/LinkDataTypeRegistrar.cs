using System.Data;

using CMS.DataEngine;
using CMS.DataEngine.Internal;
using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Content;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(LinkListFormComponent.IDENTIFIER, typeof(LinkListFormComponent), "Link List")]
[assembly: RegisterFormComponent(LinkFormComponent.IDENTIFIER, typeof(LinkFormComponent), "Link")]

namespace Kentico.Community.Portal.Admin;

public static class LinkDataTypeRegistrar
{
    public static void Register(IDataTypeRegister register)
    {
        DataTypeRegistration links = new(
            new DataType<IEnumerable<LinkDataType>>(
                sqlType: "nvarchar(max)",
                fieldType: LinkDataType.FIELD_TYPE_LIST,
                schemaType: "xs:string",
                conversionFunc: JsonDataTypeConverter.ConvertToModels,
                dbConversionFunc: JsonDataTypeConverter.ConvertToString,
                textSerializer: new DefaultDataTypeTextSerializer(LinkDataType.FIELD_TYPE_LIST))
            {
                TypeAlias = "string",
                TypeGroup = "String",
                SqlValueFormat = DataTypeManager.UNICODE,
                DbType = SqlDbType.NVarChar,
                DefaultValueCode = "[]",
                DefaultValue = [],
                HasConfigurableDefaultValue = false,
            },
            new(LinkDataType.FIELD_TYPE_LIST,
                () => new DataTypeCodeGenerator(
                    field => "IEnumerable<LinkDataType>",
                    field => nameof(ValidationHelper.GetString),
                    field => "[]",
                    field => ["System.Collections.Generic", "Kentico.Community.Portal.Core.Content"])));

        DataTypeRegistration link = new(
            new DataType<LinkDataType>(
                sqlType: "nvarchar(max)",
                fieldType: LinkDataType.FIELD_TYPE,
                schemaType: "xs:string",
                conversionFunc: JsonDataTypeConverter.ConvertToModel,
                dbConversionFunc: JsonDataTypeConverter.ConvertToString,
                textSerializer: new DefaultDataTypeTextSerializer(LinkDataType.FIELD_TYPE))
            {
                TypeAlias = "string",
                TypeGroup = "String",
                SqlValueFormat = DataTypeManager.UNICODE,
                DbType = SqlDbType.NVarChar,
                DefaultValueCode = "{ }",
                DefaultValue = new(),
                HasConfigurableDefaultValue = false,
            },
            new(LinkDataType.FIELD_TYPE,
                () => new DataTypeCodeGenerator(
                    field => "LinkDataType",
                    field => nameof(ValidationHelper.GetString),
                    field => "new()",
                    field => ["Kentico.Community.Portal.Core.Content"])));

        register.AddRegistrations(
        [
            links,
            link
        ]);
    }
}

public class LinkListFormComponentAttribute : FormComponentAttribute { }

[ComponentAttribute(typeof(LinkListFormComponentAttribute))]
public class LinkListFormComponent : FormComponent<
    LinkListFormComponentProperties,
    LinkListFormComponentClientProperties,
    IEnumerable<LinkDataType>>
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Admin.FormComponent.LinkList";

    public override string ClientComponentName => "@kentico-community/portal-web-admin/LinkListDataType";
}

public class LinkListFormComponentProperties : FormComponentProperties { }

public class LinkListFormComponentClientProperties : FormComponentClientProperties<IEnumerable<LinkDataType>>
{
    public LinkDataType NewLink { get; } = new LinkDataType();
}


public class LinkFormComponentAttribute : FormComponentAttribute { }

[ComponentAttribute(typeof(LinkFormComponentAttribute))]
public class LinkFormComponent : FormComponent<
    LinkFormComponentProperties,
    LinkFormComponentClientProperties,
    LinkDataType>
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Admin.FormComponent.Link";

    public override string ClientComponentName => "@kentico-community/portal-web-admin/LinkDataType";
}

public class LinkFormComponentProperties : FormComponentProperties { }

public class LinkFormComponentClientProperties : FormComponentClientProperties<LinkDataType>
{
    public LinkDataType NewLink { get; } = new LinkDataType();
}
