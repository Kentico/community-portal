﻿using CMS.DataEngine;
using Kentico.Community.Portal.Core.Operations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.ViewComponents.Footer;

public class FooterViewComponent(IMediator mediator) : ViewComponent
{
    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string version = await mediator.Send(new SettingXperienceVersionQuery());

        return View("~/Components/ViewComponents/Footer/Footer.cshtml", new FooterViewModel(version));
    }
}

public record FooterViewModel(string XperienceVersion);

public record SettingXperienceVersionQuery : IQuery<string>;
public class SettingXperienceVersionQueryHandler(DataItemQueryTools tools, IInfoProvider<SettingsKeyInfo> settings) : DataItemQueryHandler<SettingXperienceVersionQuery, string>(tools)
{
    private readonly IInfoProvider<SettingsKeyInfo> settings = settings;

    public override async Task<string> Handle(SettingXperienceVersionQuery request, CancellationToken cancellationToken)
    {
        var setting = await settings.GetAsync("CMSDBVersion");

        return setting.KeyValue;
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(SettingXperienceVersionQuery query, string result, ICacheDependencyKeysBuilder builder) =>
        builder.SettingsKey("CMSDBVersion");
}
