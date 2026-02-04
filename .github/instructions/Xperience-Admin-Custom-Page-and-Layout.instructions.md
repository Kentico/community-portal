---
applyTo: "**/*.cs;**/*.tsx;**/*.ts"
---

# Xperience Admin Custom Pages and Layouts

When creating custom administration pages and layouts, follow the architecture
that connects C# backend definitions to React/TypeScript client templates
through strongly-typed properties and command handlers.

## Architecture Overview

Custom admin pages consist of three interconnected components:

1. **Backend Definition** – A C# class inheriting from `ApplicationPage` (root)
   or `Page<TClientProperties>` (child pages) that manages page logic and
   commands
2. **Frontend Template** – A React component exported from the admin client
   module that renders the UI
3. **Registration** – Assembly-level attributes that tie backend and frontend
   together and register the page in the admin navigation

The flow works as follows:

- Backend `ConfigureTemplateProperties()` populates initial data and passes it
  to the React component
- Frontend component receives typed props and can invoke `PageCommand` methods
  from backend
- Commands return `ICommandResponse` with data, messages, or navigation
  instructions

## Backend Patterns

### File Organization

Backend pages belong in
`src/Kentico.Community.Portal.Admin/Features/{FeatureName}/`:

- `{FeatureName}ApplicationPage.cs` – Root application page (entry point for a
  feature section)
- `{FeatureName}Page.cs` or `{SpecificFeature}Page.cs` – Individual pages within
  the application

### UIApplication: Root Application Pages

Application pages serve as the entry point for a feature section. They are
registered with the `UIApplication` assembly attribute and use
`TemplateNames.SECTION_LAYOUT` as their template:

```csharp
using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: ExampleApplicationPage.IDENTIFIER,
    type: typeof(ExampleApplicationPage),
    slug: "example",
    name: "Example Feature",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.Settings,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.Example;

[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.UPDATE)]
public class ExampleApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "example-app";
}
```

**Key points:**

- `identifier` must be a unique string constant
- `slug` becomes the URL segment (e.g., `/admin/example`)
- `category` groups the app in admin navigation (use
  `PortalWebAdminModule.COMMUNITY_CATEGORY`)
- `[UIPermission]` attributes define who can access this application

### UIPage: Child Pages with Custom Layouts

Child pages register with the `UIPage` attribute and reference a custom React
template. They inherit from `Page<TClientProperties>` where `TClientProperties`
is a strongly-typed class containing all properties passed to the React
component:

```csharp
using CMS.Membership;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(ExampleManagementPage),
    parentType: typeof(ExampleApplicationPage),
    slug: "management",
    name: "Management",
    templateName: "@kentico-community/portal-web-admin/ExampleManagement",
    order: 1,
    Icon = Icons.Cog)]

namespace Kentico.Community.Portal.Admin.Features.Example;

public class ExampleManagementPage(IExampleService exampleService) : Page<ExampleManagementPageClientProperties>
{
    public const string LOAD_DATA_COMMAND = "LoadData";
    public const string PROCESS_COMMAND = "Process";

    public override async Task<ExampleManagementPageClientProperties> ConfigureTemplateProperties(
        ExampleManagementPageClientProperties properties)
    {
        var items = await exampleService.GetItems();
        properties.Items = items;
        properties.LoadDataCommandName = LOAD_DATA_COMMAND;
        properties.ProcessCommandName = PROCESS_COMMAND;

        return properties;
    }

    [PageCommand(CommandName = LOAD_DATA_COMMAND)]
    public async Task<ICommandResponse> LoadData()
    {
        var items = await exampleService.GetItems();
        return ResponseFrom(items)
            .AddInfoMessage("Data loaded");
    }

    [PageCommand(CommandName = PROCESS_COMMAND, Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse> Process(ProcessCommandParams commandParams)
    {
        var result = await exampleService.Process(commandParams.ItemId);

        return ResponseFrom(result)
            .AddSuccessMessage("Processing complete");
    }
}

/// <summary>
/// Properties passed from backend to React component on page load.
/// Must inherit from TemplateClientProperties and use name pattern: {PageName}ClientProperties
/// </summary>
public class ExampleManagementPageClientProperties : TemplateClientProperties
{
    public IEnumerable<ItemDto> Items { get; set; } = [];
    public string LoadDataCommandName { get; set; } = "";
    public string ProcessCommandName { get; set; } = "";
}

public record ProcessCommandParams(int ItemId);

public record ItemDto(int Id, string Name);
```

**Key points:**

- `parentType` specifies the parent `UIApplication` type
- `templateName` format is always `@{orgName}/{projectName}/{templateName}` (see
  below)
- `order` numeric value controls position in navigation menus
- Generic parameter `Page<TClientProperties>` defines the shape of data sent to
  React
- `ConfigureTemplateProperties()` runs on initial page load; populate all
  properties here
- Properties in `{PageName}ClientProperties` are serialized to JSON with
  camelCase names

### Template Name Conventions

The `templateName` property follows this pattern:

```
@{orgName}/{projectName}/{templateName}
```

From the `.csproj`:

- `{orgName}` comes from `<AdminOrgName>kentico-community</AdminOrgName>` in the
  csproj file
- `{projectName}` is the client module name from webpack config
  (`portal-web-admin` in this repo)
- `{templateName}` is the PascalCase name of the exported React component
  (without the `Template` suffix)

For example:

- `@kentico-community/portal-web-admin/MigrationManagement` → loads
  `MigrationManagementTemplate` from the admin client module
- `@kentico-community/portal-web-admin/ExampleManagement` → loads
  `ExampleManagementTemplate`

### PageCommand: Backend Command Handlers

Page commands allow the frontend to invoke backend logic. Decorate handler
methods with the `[PageCommand]` attribute:

```csharp
[PageCommand(CommandName = "MyCommand", Permission = SystemPermissions.UPDATE)]
public async Task<ICommandResponse> MyCommandHandler(CommandParams commandParams, CancellationToken ct = default)
{
    // Command logic
    var result = await DoSomething(commandParams, ct);

    return ResponseFrom(result)
        .AddSuccessMessage("Operation completed")
        .AddInfoMessage("Additional context");
}

public record CommandParams(string Name, int Value);
```

**Key points:**

- Method must be `async Task<ICommandResponse>`
- `CommandName` parameter is optional; defaults to method name if not specified
- `Permission` parameter is optional; controls authorization
- Handler can accept a strongly-typed parameter class (automatically bound from
  JSON)
- Handler can request a `CancellationToken` parameter for long-running
  operations
- Use `Response()` for simple completion signals
- Use `ResponseFrom<T>()` to return typed data to the client
- Chain message methods: `.AddSuccessMessage()`, `.AddErrorMessage()`,
  `.AddInfoMessage()`, `.AddWarningMessage()`
- Property names in parameter classes follow C# PascalCase; they're
  automatically deserialized from camelCase JSON

### Response Builders

Build command responses fluently:

```csharp
// Simple completion
return Response()
    .AddSuccessMessage("Done");

// Return data to client
return ResponseFrom(new { Id = 1, Name = "Item" })
    .AddSuccessMessage("Item created")
    .AddInfoMessage("Additional info");

// Trigger client-side command (e.g., reload data)
return Response()
    .UseCommand("LoadData")
    .AddInfoMessage("Data refreshed");

// Navigate to another page
return NavigateTo(pageLinkGenerator.GetPath(typeof(AnotherPage)))
    .AddInfoMessage("Redirecting...");
```

## Frontend Patterns

### File Organization

Frontend components belong in `src/Kentico.Community.Portal.Admin/Client/src/`:

- `layouts/` – Page templates (exported as `{Name}LayoutTemplate` or
  `{Name}Template`)
- `components/` – Reusable form/UI components
- `entry.tsx` – Module exports (all components must be exported from here for
  visibility)

### React Template Structure

Create a React component matching the backend's `TClientProperties`:

```tsx
import React, { useState } from 'react';
import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  Card,
  Headline,
  HeadlineSize,
  Input,
  Paper,
} from '@kentico/xperience-admin-components';

// Interface matches backend ExampleManagementPageClientProperties (with camelCase props)
interface ExampleManagementClientProperties {
  items: ItemDto[];
  loadDataCommandName: string;
  processCommandName: string;
}

type ItemDto = {
  id: number;
  name: string;
};

export const ExampleManagementTemplate = (
  props: ExampleManagementClientProperties,
) => {
  const [items, setItems] = useState<ItemDto[]>(props.items);
  const [selectedItemId, setSelectedItemId] = useState<number | undefined>();

  // Load data command
  const { execute: loadData, inProgress: isLoading } = usePageCommand<ItemDto[]>(
    props.loadDataCommandName,
    {
      after: (loadedItems) => {
        setItems(loadedItems);
      },
    },
  );

  // Process command with parameters
  const { execute: processItem, inProgress: isProcessing } = usePageCommand<
    ProcessResult,
    ProcessCommandParams
  >(props.processCommandName, {
    after: (result) => {
      // Handle result from backend
      setSelectedItemId(undefined);
    },
  });

  const handleProcess = async () => {
    if (selectedItemId === undefined) return;
    await processItem({ itemId: selectedItemId });
  };

  return (
    <div>
      <Headline size={HeadlineSize.L}>Example Management</Headline>
      <Paper>
        <div>
          <Button
            label="Load Data"
            onClick={() => loadData()}
            disabled={isLoading}
            inProgress={isLoading}
          />
        </div>
        {items.map((item) => (
          <Card key={item.id} headline={item.name}>
            <p>ID: {item.id}</p>
          </Card>
        ))}
        <div>
          <Input
            type="number"
            label="Item ID to Process"
            value={selectedItemId ?? ''}
            onChange={(e) => setSelectedItemId(parseInt(e.target.value) || undefined)}
          />
          <Button
            label="Process"
            onClick={handleProcess}
            disabled={selectedItemId === undefined || isProcessing}
            inProgress={isProcessing}
          />
        </div>
      </Paper>
    </div>
  );
};
```

**Key points:**

- Interface name matches backend properties class, with camelCase property names
- Export component with `Template` suffix (e.g., `ExampleManagementTemplate`)
- Use `usePageCommand<TResult>()` for commands that return data
- Use `usePageCommand<TResult, TParams>()` when sending data to backend
- `after` callback receives typed response from backend
- `execute()` function takes command parameters and invokes backend
- Import hooks from `@kentico/xperience-admin-base`
- Import UI components from `@kentico/xperience-admin-components`

### usePageCommand Hook

The `usePageCommand` hook manages client-server communication:

```tsx
const { execute, inProgress } = usePageCommand<ResponseType, ParamType>(
  "CommandName",
  {
    // Optional callback before sending command
    before: () => {
      if (!isValid()) return false; // Return false to cancel
    },

    // Optional callback after receiving response
    after: (response) => {
      // Update component state with response data
      setData(response);
    },

    // Optional: initial data to send
    data: { initialKey: "value" },

    // Optional: re-run when dependencies change
    dependencies: [dependency1, dependency2],

    // Optional: auto-execute on component mount
    executeOnMount: false,
  },
);

// Invoke command from event handler
await execute({ paramKey: "value" });
```

**Key points:**

- First generic `<ResponseType>` is required
- Second generic `<ParamType>` is optional (omit if command takes no parameters)
- `execute()` function is a Promise that resolves when command completes
- `inProgress` boolean indicates command is executing (use for disabling
  buttons, loading states)
- `after` callback fires after response received, before any UI updates
- Command name must match backend `[PageCommand(CommandName = "...")]`

### Module Exports

All components must be exported from `Client/src/entry.tsx`:

```tsx
export * from './layouts/ExampleManagementTemplate';
export * from './components/MyCustomComponent';
```

## Naming Conventions Summary

| Item                            | Convention                                            | Example                                                 |
| ------------------------------- | ----------------------------------------------------- | ------------------------------------------------------- |
| Application Page class          | `{Feature}ApplicationPage`                            | `ExampleApplicationPage`                                |
| Child Page class                | `{Feature}Page` or `{Specific}Page`                   | `ExampleManagementPage`                                 |
| Client Properties class         | `{PageClassName}ClientProperties`                     | `ExampleManagementPageClientProperties`                 |
| React template file             | `{Name}Template.tsx` or `{Name}LayoutTemplate.tsx`    | `ExampleManagementTemplate.tsx`                         |
| React template component export | `{Name}Template` or `{Name}LayoutTemplate`            | `export const ExampleManagementTemplate`                |
| Template name in registration   | PascalCase without `Template` suffix                  | `@kentico-community/portal-web-admin/ExampleManagement` |
| Command handler method          | Descriptive verb phrase                               | `LoadData()`, `ProcessItem()`                           |
| Command name constant           | UPPER_SNAKE_CASE                                      | `const LOAD_DATA_COMMAND = "LoadData"`                  |
| Parameter classes               | `{CommandName}Params` or `{CommandName}CommandParams` | `ProcessCommandParams`                                  |

## Permission Checks

### Page-Level Permissions

Gate access to an entire page:

```csharp
[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.UPDATE)]
public class MyPage : Page<MyClientProperties>
{
}
```

Multiple `[UIPermission]` attributes create an OR relationship (user needs at
least one).

### Command-Level Permissions

Gate access to specific commands:

```csharp
[PageCommand(Permission = SystemPermissions.UPDATE)]
public async Task<ICommandResponse> DeleteItem(int itemId)
{
    // Only users with UPDATE permission can call this
}
```

### Communicating Permissions to Frontend

Pass permission state to React via `ClientProperties`:

```csharp
public class MyPageClientProperties : TemplateClientProperties
{
    public bool CanDelete { get; set; }
    public bool CanCreate { get; set; }
}

public override async Task<MyPageClientProperties> ConfigureTemplateProperties(MyPageClientProperties properties)
{
    properties.CanDelete = await permissionEvaluator.HasPermission(SystemPermissions.DELETE);
    properties.CanCreate = await permissionEvaluator.HasPermission(SystemPermissions.CREATE);
    return properties;
}
```

Then disable UI elements in React based on these flags:

```tsx
<Button label="Delete" disabled={!props.canDelete} onClick={handleDelete} />
```

## Complete Minimal Example

This example demonstrates a complete admin page with initial data loading and a
command handler.

**Backend** – `Features/Example/ExampleApplicationPage.cs`:

```csharp
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: UIApplication(
    identifier: ExampleApplicationPage.IDENTIFIER,
    type: typeof(ExampleApplicationPage),
    slug: "example",
    name: "Example Feature",
    category: PortalWebAdminModule.COMMUNITY_CATEGORY,
    icon: Icons.Settings,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Community.Portal.Admin.Features.Example;

public class ExampleApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "example-app";
}
```

**Backend** – `Features/Example/ExamplePage.cs`:

```csharp
using CMS.Membership;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(ExamplePage),
    parentType: typeof(ExampleApplicationPage),
    slug: "overview",
    name: "Overview",
    templateName: "@kentico-community/portal-web-admin/ExampleOverview",
    order: 1,
    Icon = Icons.Eye)]

namespace Kentico.Community.Portal.Admin.Features.Example;

[UIPermission(SystemPermissions.VIEW)]
public class ExamplePage(IExampleService service) : Page<ExamplePageClientProperties>
{
    public const string COUNT_COMMAND = "GetCount";

    public override async Task<ExamplePageClientProperties> ConfigureTemplateProperties(
        ExamplePageClientProperties properties)
    {
        var count = await service.GetCount();
        properties.ItemCount = count;
        properties.CountCommandName = COUNT_COMMAND;
        return properties;
    }

    [PageCommand(CommandName = COUNT_COMMAND)]
    public async Task<ICommandResponse> GetCount()
    {
        var count = await service.GetCount();
        return ResponseFrom(new { count })
            .AddInfoMessage("Count refreshed");
    }
}

public class ExamplePageClientProperties : TemplateClientProperties
{
    public int ItemCount { get; set; }
    public string CountCommandName { get; set; } = "";
}
```

**Frontend** – `Client/src/layouts/ExampleOverviewTemplate.tsx`:

```tsx
import React, { useState } from 'react';
import { usePageCommand } from '@kentico/xperience-admin-base';
import { Button, Headline, HeadlineSize, Paper } from '@kentico/xperience-admin-components';

interface ExampleOverviewClientProperties {
  itemCount: number;
  countCommandName: string;
}

export const ExampleOverviewTemplate = (
  props: ExampleOverviewClientProperties,
) => {
  const [count, setCount] = useState(props.itemCount);

  const { execute: getCount, inProgress } = usePageCommand<{ count: number }>(
    props.countCommandName,
    {
      after: (response) => setCount(response.count),
    },
  );

  return (
    <Paper>
      <Headline size={HeadlineSize.L}>Example Overview</Headline>
      <p>Total items: {count}</p>
      <Button
        label="Refresh Count"
        onClick={() => getCount()}
        inProgress={inProgress}
      />
    </Paper>
  );
};
```

**Frontend** – `Client/src/entry.tsx` (add export):

```tsx
export * from './layouts/ExampleOverviewTemplate';
```

## Troubleshooting & Best Practices

### Component Not Appearing

1. Verify the template name is correct in the `[UIPage]` attribute (format:
   `@{orgName}/{projectName}/{templateName}`)
2. Ensure the React component is exported from `entry.tsx`
3. Component name must match `{templateName}Template` (e.g.,
   `ExampleOverviewTemplate` for `ExampleOverview`)
4. Check browser console for module loading errors
5. Verify `AdminOrgName` and `projectName` in `.csproj` match the template
   identifier

### Command Not Executing

1. Verify `CommandName` matches between `[PageCommand(CommandName = "...")]` and
   `usePageCommand("...", ...)`
2. Ensure the method is `async Task<ICommandResponse>`
3. Check browser console for network errors
4. Verify user has required permissions via `[PageCommand(Permission = ...)]`
5. Check the backend page is registered with `[UIPage]` assembly attribute

### Type Mismatch Between Backend and Frontend

1. Backend `ClientProperties` properties use PascalCase (e.g., `ItemCount`)
2. Frontend interface properties must use camelCase (e.g., `itemCount`)
3. Serialization/deserialization handles the conversion automatically
4. Use `ResponseFrom<T>()` with matching type on both sides

### Performance Considerations

1. Only load necessary data in `ConfigureTemplateProperties()` – avoid full
   dataset loads
2. Use `async/await` throughout command handlers
3. Request `CancellationToken` parameter in long-running commands for graceful
   shutdown
4. Implement pagination or filtering for large datasets
5. Leverage React `useState` and `useCallback` to avoid unnecessary re-renders

## References

- [Xperience UI Pages Documentation](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages)
- [Xperience UI Page Commands Documentation](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages/ui-page-commands)
- [Xperience UI Page Permission Checks](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/ui-pages/ui-page-permission-checks)
- [Xperience Dependency Injection](https://docs.kentico.com/documentation/developers-and-admins/development/website-development-basics/dependency-injection)
