using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kentico.Community.Portal.Web.Features.Errors;

public class GlobalExceptionFilter(IModelMetadataProvider modelMetadataProvider) : IExceptionFilter
{
    private readonly IModelMetadataProvider modelMetadataProvider = modelMetadataProvider;

    public void OnException(ExceptionContext context)
    {
        var result = new ViewResult
        {
            ViewName = "~/Features/Errors/Exception.cshtml",
            ViewData = new ViewDataDictionary(modelMetadataProvider, context.ModelState)
            {
                Model = new ErrorPageViewModel
                {
                    StatusCode = 500
                }
            },
            StatusCode = 500
        };

        context.ExceptionHandled = true;
        context.Result = result;
    }
}
