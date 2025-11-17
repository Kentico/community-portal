---
description: Reports repository infrastructure dependencies for local development
agent: agent
---

# Report Local Development Dependencies

Analyze and report all infrastructure dependencies required for local
development of this repository.

## Sources to Check

- `global.json` - .NET SDK version requirement
- `Directory.Build.props` - .NET TFM/framework version for projects
- `package.json` files - Node.js version from `engines` property
- `README.md` - General setup overview
- `docs/Required-Software.md` - Comprehensive software requirements
- `docs/Environment-Setup.md` - Environment configuration details
- Xperience by Kentico official documentation system requirements

## Report Format

Generate a structured report with these sections:

### Runtime Requirements

- **.NET SDK**: Version from `global.json`
- **.NET Target Framework**: Version from `Directory.Build.props`
- **Node.js**: Version from `package.json` engines

### Database

- SQL Server version requirements
- Docker alternative options
- Connection requirements

### Development Tools

- Required IDEs/editors
- PowerShell version
- SQL editors

### Storage & Services

- Azure Storage/Azurite requirements
- Email SMTP server (MailHog/Mailpit)

### Xperience by Kentico Specifics

- Required Xperience version
- Admin client requirements
- License requirements

### Operating System Support

- Supported platforms (Windows/macOS/Linux)
- Platform-specific considerations

## Output Style

- Use clear, concise bullet points
- Include version numbers explicitly
- Highlight cross-platform vs platform-specific requirements
- Note recommended vs alternative options
- Include relevant documentation links where applicable
