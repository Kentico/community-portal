using System.Security.Claims;

namespace Kentico.Community.Portal.Web.Infrastructure;

public interface IFormBuilderContext
{
    public FormBuilderMode Mode { get; }
}

public class FormBuilderContext(IHttpContextAccessor contextAccessor) : IFormBuilderContext
{
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    public FormBuilderMode Mode
    {
        get
        {
            var ctx = contextAccessor.HttpContext;

            if (ctx is null)
            {
                return FormBuilderMode.None;
            }

            bool isUserAuthenticated = ctx.User.Identity is ClaimsIdentity claimsIdentity && claimsIdentity.IsAuthenticated;

            if (!isUserAuthenticated)
            {
                return FormBuilderMode.Live;
            }

            if (isUserAuthenticated
                && ctx.Request.Path.StartsWithSegments(PathString.FromUriComponent("/Kentico.FormBuilder/FormItem/Preview"), StringComparison.OrdinalIgnoreCase))
            {
                return FormBuilderMode.ValueEditor;
            }

            if (isUserAuthenticated
                && ctx.Request.Path.StartsWithSegments(PathString.FromUriComponent("/kentico.formbuilder/markup/editorrow"), StringComparison.OrdinalIgnoreCase))
            {
                return FormBuilderMode.BuilderEditor;
            }

            return FormBuilderMode.Live;
        }
    }
}

public enum FormBuilderMode
{
    None,
    Live,
    BuilderEditor,
    ValueEditor
}
