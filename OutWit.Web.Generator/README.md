# OutWit.Web.Generator

Part of [WitDocs](https://witdocs.io) — a .NET CLI tool that generates static content for WitDocs sites: content/navigation/metadata indices, sitemap, robots.txt, RSS feed, search index, pre-rendered (syntax-highlighted) static HTML pages, OG images, and hosting-provider config.

## Installation

```bash
dotnet tool install -g OutWit.Web.Generator
```

## Usage

### Automatic Generation (Recommended)

When using **OutWit.Web.Framework**, content generation runs automatically on **Release builds**:

1. Install the Generator tool globally
2. In Visual Studio, switch to **Release** configuration
3. Build your project (Ctrl+Shift+B)
4. Generation runs automatically after build!

### Manual CLI Usage

```bash
# --site points at the project dir; --output defaults to <site>/wwwroot
outwit-generate --site ./MySite --url https://mysite.com --hosting cloudflare
```

## CLI Options

| Option | Description | Default |
|--------|-------------|---------|
| `-s, --site` | Path to the site project directory | **Required** |
| `-o, --output` | Output directory | `<site>/wwwroot` |
| `-u, --url` | Site URL (read from `site.config.json` if omitted) | (from config) |
| `-h, --hosting` | Hosting provider (cloudflare/netlify/vercel/github/none) | `cloudflare` |
| `--skip-sitemap` | Skip sitemap.xml + robots.txt | false |
| `--skip-search` | Skip search index | false |
| `--skip-rss` | Skip RSS feed | false |
| `--skip-static` | Skip static HTML pages | false |
| `--og-images` | Generate OG images (requires Playwright) | false |
| `--force-og` | Force regenerate OG images | false |
| `--search-content-max-length` | Max content length per search entry | 10000 |

## MSBuild Properties

When using the Framework, configure generation via MSBuild properties in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Site URL (required for sitemap/RSS) -->
  <OutWitSiteUrl>https://example.com</OutWitSiteUrl>
  
  <!-- Site name (for RSS feed) -->
  <OutWitSiteName>My Site</OutWitSiteName>
  
  <!-- Hosting provider: cloudflare, netlify, vercel, github, none -->
  <OutWitHostingProvider>cloudflare</OutWitHostingProvider>
  
  <!-- Enable/disable specific features -->
  <OutWitGenerateStaticPages>true</OutWitGenerateStaticPages>
  <OutWitGenerateSearchIndex>true</OutWitGenerateSearchIndex>
  <OutWitGenerateRssFeed>true</OutWitGenerateRssFeed>
  <OutWitGenerateOgImages>false</OutWitGenerateOgImages>
  
  <!-- Force generation even in Debug mode -->
  <OutWitGenerateContent>true</OutWitGenerateContent>
  
  <!-- Enable generation in Debug mode (v1.2.0+) -->
  <OutWitGenerateInDebug>true</OutWitGenerateInDebug>
</PropertyGroup>
```

## Generated Files

| File | Description |
|------|-------------|
| `content/index.json` | Content manifest listing all files |
| `navigation-index.json` | Pre-built navigation menu data |
| `content-metadata.json` | Pre-built content metadata for fast list rendering |
| `sitemap.xml` | SEO sitemap |
| `robots.txt` | Crawler rules |
| `search-index.json` | Pre-built search index |
| `feed.xml` | RSS feed for blog |
| `*/index.html` | Static HTML pages |

### Hosting Provider Files

| Provider | Files Generated |
|----------|-----------------|
| **Cloudflare** | `_headers`, `_routes.json` |
| **Netlify** | `_headers`, `_redirects` |
| **Vercel** | `vercel.json` |
| **GitHub Pages** | `.nojekyll`, `404.html` |

## Features

### Content Index
Generates `index.json` listing all content files by category (blog, projects, docs, articles, features) and dynamic sections.

### Navigation Index (v1.1.0+)
Generates `navigation-index.json` for instant menu rendering without parsing markdown files.

### Content Metadata Index (v1.2.0+)
Generates `content-metadata.json` for fast list page rendering (BlogListPage, HomePage) without parsing all markdown files.

### Sitemap
Creates `sitemap.xml` and `robots.txt` with proper lastmod dates.

### Search Index
Generates `search-index.json` for client-side search functionality.

### RSS Feed
Creates `feed.xml` for blog posts with proper formatting.

### Static HTML
Pre-renders HTML pages for SEO and faster initial load.

### OG Images
Generates Open Graph images for social sharing using Playwright.

```bash
# Install Playwright before using OG images
npx playwright install chromium
```

## Dynamic Content Sections

Define custom content sections in `site.config.json`:

```json
{
  "contentSections": [
    { "folder": "solutions", "route": "solutions", "menuTitle": "Solutions" }
  ]
}
```

## GitHub Actions

Example workflow step:

```yaml
- name: Install Generator
  run: dotnet tool install -g OutWit.Web.Generator

- name: Publish + generate
  run: |
    dotnet publish MySite/MySite.csproj -c Release -o publish -p:OutWitGenerateContent=false
    outwit-generate --site MySite --output publish/wwwroot --url https://example.com --hosting cloudflare
```

## What's New

### v1.4.x
- **Syntax-highlighted static pages**: the SSG output (and the live app) highlight
  fenced code via the framework's build-time highlighter (ColorCode) — no client JS.
- **Trailing-slash canonical consistency**: `sitemap.xml` `<loc>`, RSS `<link>`/`<guid>`
  and pre-rendered internal links use the final 200 URL (trailing slash).
- **No-JS-readable static pages**: pre-rendered content is visible without JS; the
  spinner is shown only when JS is available (no "JavaScript required" dead-end).
- **Hosting headers**: the Blazor boot loaders (`dotnet.js`, `blazor.webassembly.js`)
  are emitted with `no-cache` (content-hashed assets stay `immutable`), so deploys
  are never pinned to a stale build.

### v1.3.x
- Cloudflare `_routes.json` for SPA routing; navigation + content-metadata indices;
  markdown rendering fixes for Description/Summary.

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
