---
agent: "agent"
tools: ["search/codebase", "usages"]
description: "Instructions to create a Page Builder widget"
---

A Page Builder widget...

- Is a class that inherits from `ViewComponent` and has a class name suffixed
  with "Widget"
- Has a properties class implementing `IWidgetProperties`, matching the name of
  the widget suffixed with "Properties"
- Has a view model class matching the name of the widget, suffixed with
  "ViewModel"
- Validates its properties before rendering the view. Example:

  ```csharp
  private static Result<ImageWidgetViewModel, ComponentErrorViewModel> Validate(ImageWidgetProperties props, Maybe<ImageContent> image)
  {
      if (props.SelectedImages.FirstOrDefault() is null)
      {
          return Result.Failure<ImageWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No image has been selected."));
      }

      if (!image.TryGetValue(out var img) || img.ImageContentAsset is null)
      {
          return Result.Failure<ImageWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item or image file no longer exists."));
      }

      return new ImageWidgetViewModel(ImageAssetViewModel.Create(img), props);
  }
  ```

- If the widget properties or data fails validation, display the
  `ComponentError.cshtml` view. Example:

  ```csharp
  return Validate(props, image)
  .Match(
      vm => View("~/Components/PageBuilder/Widgets/Image/Image.cshtml", vm),
      vm => View("~/Components/ComponentError.cshtml", vm));
  ```

- Uses C# primary constructors
- The widget, properties, and view model classes should all be in the same file
- Has a Razor view named the same as the widget without the "Widget" suffix
- Both the Widget C# class and Razor view are located in a folder for the
  widget, and the folder should be in the
  ./src/Kentico.Community.Portal.Web/Components/Widgets folder and not include
  the "Widget" suffix
