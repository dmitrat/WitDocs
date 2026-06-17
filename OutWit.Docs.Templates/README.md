# OutWit.Docs.Templates

Part of [WitDocs](https://witdocs.io) — project templates for creating static documentation/content sites with the WitDocs framework.

## Installation

```bash
dotnet new install OutWit.Docs.Templates
```

## Usage

### Create a new site

```bash
# Basic usage
dotnet new witdocs -n MySite

# With all options
dotnet new witdocs -n MySite \
  --siteName "My Awesome Site" \
  --siteDescription "Docs and guides for My Awesome Site" \
  --baseUrl "https://mysite.com" \
  --authorName "John Doe" \
  --accentColor "#FF6B6B" \
  --githubUrl "https://github.com/johndoe" \
  --twitterHandle "@johndoe" \
  --hostingProvider cloudflare
```

### Available Options

| Option | Description | Default |
|--------|-------------|---------|
| `--siteName` | Display name of your site | My Site |
| `--siteDescription` | Description for SEO / social sharing | Welcome to my site |
| `--baseUrl` | Base URL (e.g., https://example.com) | https://example.com |
| `--authorName` | Author name for copyright | Your Name |
| `--accentColor` | Primary accent color (hex) | #007CF0 |
| `--githubUrl` | GitHub profile/repo URL | (empty) |
| `--twitterHandle` | Twitter handle | (empty) |
| `--hostingProvider` | Target hosting (cloudflare/netlify/vercel/github/none) | cloudflare |
| `--includeDocsSection` | Include docs pages | false |
| `--includeBlogSection` | Include blog pages | true |
| `--includeProjectsSection` | Include projects pages | true |
| `--enableDebugGeneration` | Generate content indices in Debug builds too | false |

### Run your site

```bash
cd MySite
dotnet run
```

### Build for production

```bash
dotnet publish -c Release
```

The template ships a local tool manifest (`.config/dotnet-tools.json`) pinning the
generator, so a Release build restores and runs it automatically — **no global
`dotnet tool install` required**. It also includes a `.gitignore` that excludes the
generated SEO/content assets.

## Project Structure

```
MySite/
  Pages/
    Index.razor
    Blog.razor
    BlogPost.razor
    Project.razor
    Contact.razor
    Search.razor
  wwwroot/
    content/
      blog/
      projects/
    css/
      theme.css
      site.css
    images/
    site.config.json
    index.html
  Program.cs
  MySite.csproj
```

## Customization

1. **Colors**: Edit `wwwroot/css/theme.css`
2. **Navigation**: Edit `wwwroot/site.config.json`
3. **Content**: Add markdown files to `wwwroot/content/` folders
4. **Logo**: Replace `wwwroot/images/logo.svg`

## What's New

### v1.4.x
- **Syntax-highlighted code blocks** with a copy button (pure C#, no JS to write).
- **Pluggable markdown components** — register your own with
  `services.AddContentComponent<T>("Name")` and use `[[Name ...]]` in markdown.
- **Smoother / SEO-friendly loading**: inline theme background (no white flash),
  no-JS-readable pre-rendered content, trailing-slash canonical URLs.
- **Local tool manifest** so new projects build without a global generator install;
  shipped `.gitignore` for generated assets; `siteDescription` template option.

### v1.3.x
- Skeleton loading components, content-metadata index for fast lists, optional
  Debug-mode generation, and direct single-item content loading.

## Uninstall

```bash
dotnet new uninstall OutWit.Docs.Templates
```

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use WitDocs in a product, a mention is appreciated (but not required), for example:
"Built with [WitDocs](https://witdocs.io)".

## Trademark / Project name

"WitDocs" and "OutWit" are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with WitDocs");
- use the name to indicate compatibility (e.g., "WitDocs-compatible").

You may not:
- use "WitDocs" as the name of a fork or a derived product in a way that implies it is the official project;
- use the WitDocs logo to promote forks or derived products without permission.
