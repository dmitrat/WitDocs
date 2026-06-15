# Changelog

All notable changes to the WitDocs packages (OutWit.Web.Framework,
OutWit.Web.Generator, OutWit.Web.Templates) are documented here.

## 1.3.5

### SEO / static site generation
- **Home page is now pre-rendered** for crawlers (site header + project list +
  recent posts with internal links). Previously the root page served only the
  empty SPA shell, so search engines saw "Loading…".
- Section list pages (blog/articles/docs/custom sections) are pre-rendered with
  real links; `contact`/`search` always emitted; empty sections skipped.
- Fixed static-page corruption: the `#app` content is now replaced with a
  depth-counting matcher instead of a non-greedy regex that broke on nested
  `<div>`s (e.g. the loading indicator), which left orphaned markup.
- The generator now reuses the framework's Markdig pipeline, so static HTML
  matches the live app (auto heading ids/anchors, task lists, emoji, frontmatter).
- sitemap.xml and RSS now XML-escape URLs; JSON-LD escapes `<` to avoid
  `</script>` breakout.

### Extensibility
- **Custom markdown components**: register your own component with
  `services.AddContentComponent<TComponent>("Name")` and embed it in markdown as
  `[[Name ...]]` — no framework changes required. Built-ins (YouTube, Svg,
  FloatingImage) still work and can be overridden by name.
- Static site generation degrades embedded components gracefully (block
  components keep inner content, self-closing are removed).

### Security
- `SiteConfig.AllowRawHtml` (default `true`) — set to `false` to strip raw HTML
  (e.g. `<script>`) from rendered markdown with no extra dependency / payload.
- YouTube embeds encode their attributes; sitemap/RSS/JSON-LD escaping (above).

### Reliability / performance
- Content loaders fetch a section's files in parallel (was serial), removing the
  per-file round-trip bottleneck.
- Markdown is no longer rendered twice per file (added `GetFrontmatter<T>`).
- Fixed a race in `GetDocsAsync` (now locked) and a shared-counter race in the
  singleton `ContentParser`.
- The generator returns a non-zero exit code on fatal failure (was always 0).
- YAML / site.config parse failures are logged instead of silently swallowed.

### Build / tooling
- The project template ships a local tool manifest (`.config/dotnet-tools.json`)
  pinning the generator, and the targets run `dotnet tool restore` automatically,
  so `dotnet new outwit-web` + `dotnet build -c Release` works without a global
  tool install. Build emits an actionable error if the tool is missing.
- The template now ships a `.gitignore` that excludes generated content/SEO
  assets (sitemap, search index, feed, og-images, hosting config).

### CI
- Fixed `test.yml` (referenced a non-existent `OutWit.sln`; now `OutWit.slnx`).
- Fixed a typo that prevented symbol packages from being pushed; `publish`/`pack`
  now run the test suite as a gate.

### Tests
- Added `ContentService` tests (fake `HttpMessageHandler`), an end-to-end
  generation-pipeline integration test, component-registry and SSG-degradation
  tests, and markdown raw-HTML policy tests. 153 tests total.

## 1.3.4 and earlier

See git history. 1.3.x introduced pre-built navigation/metadata indices, direct
single-item loading, debug-mode generation, and skeleton loading components.
