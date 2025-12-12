---
agent: "agent"
tools: ["search/codebase", "usages"]
description: "Instructions to create a Page Builder section"
---

A Page Builder section...

- Is a class that inherits from `ViewComponent` and has a class name suffixed
  with "Section"
- Has a properties class implementing `ISectionProperties`, matching the name of
  the section suffixed with "Properties"
- Has a view model class matching the name of the section, suffixed with
  "ViewModel"
- Uses C# primary constructors
- Is typically simple enough to generate an instance of its view model directly
  from its properties
- The section, properties, and view model classes should all be in the same file
- Has a Razor view named the same as the section without the "Section" suffix
- Both the section C# class and Razor view are located in a folder for the
  section, and the folder should be in the
  ./src/Kentico.Community.Portal.Web/Components/Sections folder and not include
  the "Section" suffix
- Sections are used for web page layout and their Razor views include
  `<widget-zone name="zoneName" />` elements to specify where widgets are
  allowed to be placed on the page in the section.
