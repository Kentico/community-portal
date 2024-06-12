using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(PortalWebAdminModule))]

[assembly: UICategory(
   codeName: PortalWebAdminModule.QANDA_CATEGORY,
   name: "Q&A",
   icon: Icons.Personalisation,
   order: 100)]

[assembly: UICategory(
   codeName: PortalWebAdminModule.SUPPORT_REQUESTS_CATEGORY,
   name: "Support Requests",
   icon: Icons.File,
   order: 100)]

[assembly: UICategory(
   codeName: PortalWebAdminModule.STATS_CATEGORY,
   name: "Stats",
   icon: Icons.Graph,
   order: 100)]

namespace Kentico.Community.Portal.Admin;

internal class PortalWebAdminModule : AdminModule, IDataTypeRegister
{
    public const string QANDA_CATEGORY = "kentico-community.portal-web-admin.q-and-a";
    public const string SUPPORT_REQUESTS_CATEGORY = "kentico-community.portal-web-admin.support-requests";
    public const string STATS_CATEGORY = "kentico-community.portal-web-admin.stats";

    public PortalWebAdminModule() : base(nameof(PortalWebAdminModule)) { }

    protected override void OnPreInit(ModulePreInitParameters parameters)
    {
        base.OnPreInit(parameters);

        LinkDataTypeRegistrar.Register(this);
        UTMParametersDataTypeRegistrar.Register(this);
    }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("kentico-community", "portal-web-admin");
    }

    public void AddRegistrations(DataTypeRegistration[] registrations)
    {
        foreach (var registration in registrations)
        {
            DataTypeManager.RegisterDataTypes([registration.DataType]);
            if (registration.DataTypeCodeGenerator is { } generator)
            {
                DataTypeCodeGenerationManager.RegisterDataTypeCodeGenerator(generator.DataType, generator.GeneratorInitializer);
            }
            if (registration.DataType.HasConfigurableDefaultValue && registration.DefaultComponent is { } component)
            {
                RegisterDefaultValueComponent(component.DataType, component.ComponentIdentifier, component.SerializationFunction, component.DeserializationFunction);
            }
        }
    }
}

public interface IDataTypeRegister
{
    void AddRegistrations(DataTypeRegistration[] registrations);
}

public record DataTypeRegistration(
    DataType DataType,
    DataTypeCodeGeneratorRegistration? DataTypeCodeGenerator,
    DataTypeDefaultComponentRegistration? DefaultComponent)
{
    public DataTypeRegistration(DataType dataType) : this(dataType, null, null) { }
    public DataTypeRegistration(DataType dataType, DataTypeCodeGeneratorRegistration? dataTypeCodeGenerator) : this(dataType, dataTypeCodeGenerator, null) { }
}

public record DataTypeDefaultComponentRegistration(
    string DataType,
    string ComponentIdentifier,
    Func<object, string> SerializationFunction,
    Func<string, object> DeserializationFunction);

public record DataTypeCodeGeneratorRegistration(
    string DataType,
    Func<DataTypeCodeGenerator> GeneratorInitializer);
