# Contributing

## Workflow

1. Create a new branch with one of the following prefixes
  
    - `feat/` - for new functionality
    - `refactor/` - for restructuring of existing features
    - `fix/` - for bugfixes

1. Commit changes, with a commit message preferably following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary) convention.

1. Once ready, create a PR on GitHub. The PR will need to have all comments resolved and all tests passing before it will be merged.

   - The PR should have a helpful description of the scope of changes being contributed.
   - Include screenshots or video to reflect UX or UI updates
   - Indicate if new settings need to be applied when the changes are merged - locally or in other environments

## Deployment

To deploy the ASP.NET Core application to the SaaS environment, follow the Xperience docs for [Deploying to the SaaS environment](https://docs.xperience.io/x/IgKQC).

The contents of the deployment package can be controlled by modifying the `.\src\Kentico.Community.Portal.Web\App_Data\CDRepository\repository.config` file. Changes to this file, and the CD migration scripts are tracked by the repository.
