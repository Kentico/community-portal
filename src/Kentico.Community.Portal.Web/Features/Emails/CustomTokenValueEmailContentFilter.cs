using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing.Internal;

namespace Kentico.Community.Portal.Web.Features.Emails;


public class CustomTokenValueDataContext : FormAutoresponderEmailDataContext
{
    public Dictionary<string, string> Items { get; set; } = [];
}

public class CustomTokenValueEmailContentFilter : IEmailContentFilter
{
    public Task<string> Apply(
        string text,
        EmailConfigurationInfo email,
        IEmailDataContext dataContext)
    {
        if (dataContext is CustomTokenValueDataContext customValueContext)
        {
            foreach (var (key, val) in customValueContext.Items)
            {
                text = text.Replace(key, val);
            }
        }

        return Task.FromResult(text);
    }
}
