# Headless channels

Headless channels can be accessed by following
[the Xperience by Kentico documentation](https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval/retrieve-headless-content#prepare-graphql-queries).

This project has a custom headless channel URL prefix, which can be found in the
`CMSHeadless.GraphQlEndpointPath` setting in `appsettings.json`.

> Note: This path could change in the future, so refer to the settings for the
> latest value.

Introspection is enabled for `Development` environments, but not in others. To
query the GraphQL API, use the Xperience documentation setup instructions for
the [embedded Nitro dashboard](https://chillicream.com/docs/nitro?ref=winstall)
available over the `/ui` headless endpoint path suffix.

> Example Nitro URL:
>
> - <https://localhost:45039/api/headless/ui/>

> Example GraphQL HTTP Endpoint (including channel identifier):
>
> - <https://localhost:45039/api/headless/adf3284e-e886-4c35-965b-b921e17baf8a>

You can generate your own custom API for local development and testing, which
will not be committed to source control. Or you can use the local development
API key already included in the database `.bak` backup which can be found under
the `__CMSHeadless_LOCAL.APIKey` setting in `appsettings.Development.json`.
