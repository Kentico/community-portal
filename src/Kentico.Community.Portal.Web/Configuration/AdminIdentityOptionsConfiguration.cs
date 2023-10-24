using Kentico.Membership;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class AdminIdentityOptionsConfiguration : IPostConfigureOptions<AdminIdentityOptions>
{
    private readonly IConfiguration configuration;

    public AdminIdentityOptionsConfiguration(IConfiguration configuration) =>
        this.configuration = configuration;

    public void PostConfigure(string name, AdminIdentityOptions options) =>
        options.EmailOptions.SenderAddress = configuration["DefaultSenderAddress"];
}
