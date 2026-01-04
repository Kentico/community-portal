---
description: "Prompt that helps with preparation of Widget creation process."
tools: ["edit", "search", "execute/getTerminalOutput", "execute/runInTerminal", "read/terminalLastCommand", "read/terminalSelection", "kentico.docs.mcp/*", "read/problems", "web/fetch", "todo"]
---

You are tasked with the process of creating a new prompt for generating a new
widget.

## User Input

When started, you have been provided with the path to the folder, which contains
user input files. These files contain requirements and design for the new
widget. You must follow these when creating the final prompt.

!In case the user didn't provide any path, ask them to provide it before
proceeding!

## Steps to follow

- First, check all documentation links in the
  `./instructions/docs.instructions.md` file using Kentico Docs MCP.

- Next, read all remaining files in the `./instructions/` folder.

- Then, check all requirements and design files in the user-input folder, whose
  path the user has provided to you.

- Check the current state of the project for resources you will need for
  creation of the widget. If you find already present widgets, follow their
  patterns and conventions.

- Finally, create a new instructions file in the user-input folder that will
  allow you to generate a new widget. Use
  `./create-instructions/CREATION_TEMPLATE.instructions.md` as a base and fill
  in all the parts in brackets. Other parts of the file must stay the same as in
  the template.
