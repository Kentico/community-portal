using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kentico.Community.Portal.Web.Features.Errors;

public class CustomExceptionFilter : IAsyncExceptionFilter
{
    private readonly IModelMetadataProvider modelMetadataProvider;
    private readonly IMediator mediator;

    public CustomExceptionFilter(
        IModelMetadataProvider modelMetadataProvider,
        IMediator mediator)
    {
        this.modelMetadataProvider = modelMetadataProvider;
        this.mediator = mediator;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var resp = await mediator.Send(new WebsiteSettingsContentQuery());
        var settings = resp.Settings;

        var result = new ViewResult
        {
            ViewName = "~/Features/Errors/Exception.cshtml",
            ViewData = new ViewDataDictionary(modelMetadataProvider, context.ModelState)
            {
                Model = new ErrorPageViewModel
                {
                    MessageHTML = new(settings.WebsiteSettingsContentExceptionContentHTML)
                }
            },
            StatusCode = 500
        };

        context.ExceptionHandled = true; // mark exception as handled
        context.Result = result;
    }
}
