using System.Dynamic;
using System.Globalization;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.OnlineForms;
using CSharpFunctionalExtensions;
using CsvHelper;
using Kentico.Community.Portal.Admin.Features.Forms;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;

[assembly: PageExtender(typeof(FormSubmissionsTabExtender))]

namespace Kentico.Community.Portal.Admin.Features.Forms;

[UIPermission(
    KenticoCommunityPermissions.EXPORT.Name,
    KenticoCommunityPermissions.EXPORT.DisplayName)]
public class FormSubmissionsTabExtender(
    IUIPermissionEvaluator permissionEvaluator) : PageExtender<FormSubmissionsTab>
{
    public const string EXPORT_COMMAND = "EXPORT";

    private readonly IUIPermissionEvaluator permissionEvaluator = permissionEvaluator;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var columnConfigs = Page.PageConfiguration.ColumnConfigurations;

        columnConfigs
            .TryFirst(c => string.Equals(c.Name, "FormInserted", StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.Sorting.DefaultDirection = SortTypeEnum.Desc);

        await GetFormDataFromPage()
            .Execute(async d =>
            {
                int count = await BizFormItemProvider
                    .GetItems(d.dataClass.ClassName)
                    .GetCountAsync();

                if (count == 0)
                {
                    return;
                }

                var component = new DataExportComponent()
                {
                    Properties = new DataExportProperties
                    {
                        FileNamePrefix = d.form.FormName ?? "formdata",
                        CommandName = EXPORT_COMMAND
                    }
                };

                var result = await permissionEvaluator.Evaluate(KenticoCommunityPermissions.EXPORT.Name);

                _ = Page.PageConfiguration
                    .HeaderActions
                    .AddActionWithCustomComponent(
                        component,
                        $"Export {count} record{(count == 1 ? "" : "s")}",
                        disabled: !result.Succeeded);
            });
    }

    [PageCommand(
        CommandName = EXPORT_COMMAND,
        Permission = KenticoCommunityPermissions.EXPORT.Name)]
    public async Task<ICommandResponse> ExportCommandHandler(CancellationToken cancellationToken = default)
    {
        var data = await GetFormDataFromPage(cancellationToken);

        if (!data.TryGetValue(out var d))
        {
            return ResponseFrom(DataExportResponse.Error("Could not retrieve form details"));
        }

        var dcs = await BizFormItemProvider.GetItems(d.dataClass.ClassName)
            .OrderByDescending(nameof(BizFormItem.FormInserted))
            .GetDataContainerResultAsync(cancellationToken: cancellationToken);

        var dataItems = new List<dynamic>();

        foreach (var dc in dcs)
        {
            dynamic ds = new ExpandoObject();
            var dict = (IDictionary<string, object>)ds;
            foreach (string column in dc.ColumnNames)
            {
                dict[column] = dc.TryGetValue(column, out object? value)
                ? value?.ToString() ?? ""
                : "";
            }

            dataItems.Add(ds);
        }

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(dataItems);
        writer.Flush();
        ms.Position = 0;

        return ResponseFrom(DataExportResponse.Data(Convert.ToBase64String(ms.ToArray())));
    }

    private async Task<Maybe<(BizFormInfo form, DataClassInfo dataClass)>> GetFormDataFromPage(CancellationToken cancellationToken = default)
    {
        var forms = await BizFormInfoProvider.ProviderObject.Get()
            .WhereEquals(nameof(BizFormInfo.FormID), Page.FormId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return forms
            .TryFirst()
            .Map(f => (f, DataClassInfoProvider.GetDataClassInfo(f.FormClassID)));
    }
}
