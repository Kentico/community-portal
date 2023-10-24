# Run Tests

There are multiple test settings files (`*.runsettings`) in the `\test` folder.
Each of these is used for a different type of test.
You test the entire .NET solution but specify which types of tests you want to run by selecting the settings:

   ```powershell
   dotnet test -s .\test\basic.runsettings
   ```

| Settings File        | Purpose                                             |
|----------------------|-----------------------------------------------------|
| `basic.runsettings`  | Runs only unit and integration tests, not E2E tests |
| `e2e.runsettings`    | Runs all tests, including E2E tests                 |

## Integration

To run tests for your environment you can create `Tests.Local.config` files in the test projects that are copies of the existing files, but with your own settings.

These files are ignored by source control, so any new setting keys/entries should be added to the tracked `Tests.Local.config` files.

## E2E

This project uses [Playwright](https://playwright.dev/dotnet/) for end-to-end (E2E) tests.

You can use the VS Code task `.NET Test - Install Playwright Dependencies` or the PowerShell script `.\scripts\Download-PlaywrightBrowsers.ps1`
to install the Playwright browsers used to run the E2E tests.

The `Kentico.Community.Portal.Web` project needs to be running locally if you want to run the E2E tests. In the CI environment, the GitHub Action takes care of orchestration.
