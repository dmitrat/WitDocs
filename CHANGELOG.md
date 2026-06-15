# Changelog

All notable changes to the WitDocs packages (OutWit.Web.Framework,
OutWit.Web.Generator, OutWit.Web.Templates) are documented here.

## 1.4.2

### Fixes (Generator — hosting config)
- Fixed stale-app-after-deploy on Cloudflare/Netlify. The generated `_headers`
  marked all of `/_framework/*` as `immutable` (1 year) — including the
  stable-named boot entry points `dotnet.js` and `blazor.webassembly.js`, which
  change every deploy. Browsers and the CDN edge then pinned the SPA to an old
  build (old asset hashes), so new code/styles silently failed to load for some
  sites/visitors. These two files now use `Cache-Control: no-cache` (revalidate);
  the content-hashed assets stay immutable.

> After upgrading the generator and redeploying, purge the CDN cache once to
> evict the previously-immutable boot files.

## 1.4.1

### Fixes (Framework)
- Code blocks no longer render with large top/bottom gaps. The page content
  styles (`.blog-post__content pre` / `.article-content__body pre`) load after the
  framework CSS and re-added a nested box with `margin: 1.5rem 0`; the code-block
  `pre` rule now outranks them so the wrapper alone frames the code.

## 1.4.0

### Code blocks (docs-parity)
- **Syntax highlighting** for fenced code blocks, done in C# at render time
  (ColorCode) — appears in both the static (SSG) and live output, with no
  client-side highlighter and no flash. Theme-aware token colors (light/dark).
  Supported: C#, JS, TS, JSON, HTML, XML, CSS, SQL, PowerShell, Python, C/C++,
  Java, F#, VB, PHP; unknown languages fall back to plain escaped code.
- **Copy button** on every code block (a single delegated listener in the
  framework JS; the author writes no JS).

### SEO
- **Trailing-slash canonical consistency**: canonical, og:url, sitemap `<loc>`,
  RSS `<link>`/`<guid>` and the pre-rendered internal links all point to the final
  200 URL (with trailing slash) instead of the URL that 308-redirects.
- **No-JS-readable content**: pre-rendered content is now visible without
  JavaScript (and to crawlers from source); a tiny inline script swaps in the
  loading indicator only when JS is available. Removed the "JavaScript Required"
  noscript dead-end (wrong signal for an SSG site; could leak into snippets).

## 1.3.8

### Templates
- The template's `index.html` now paints the theme background via a tiny inline
  `<style>` before external CSS loads, eliminating a white flash on first paint
  (the theme stylesheet is pulled in via `@import`, which loads after the initial
  render). Keep the inline colors in sync with `css/theme.css --color-background`.
- The same inline `<style>` fades the layout (`.site-wrapper`) in on first render,
  smoothing the moment Blazor hydrates (no abrupt header/footer flash).

Templates only; Framework (1.3.5) and Generator (1.3.7) unchanged. The same
one-line fix was applied to the existing sites' `index.html`.

## 1.3.7

### Static site generation (Generator)
- Eliminated the flash of unstyled/unhydrated content on load: the generator now
  keeps the template's loading indicator visible and places the pre-rendered
  content in a `hidden` block. Crawlers still read it from the HTML source (and
  JS-capable bots render the real SPA), while users only ever see the spinner and
  then the hydrated UI. Supersedes the 1.3.6 approach (which rendered the cards
  visibly and could briefly show them before CSS/hydration).

Generator and Templates only; Framework is unchanged at 1.3.5. Existing sites
pick up the fix on their next deploy (they install the generator tool at
`latest`); no site project changes are required.

## 1.3.6

### Static site generation (Generator)
- First attempt at fixing the home/list prerender flash by emitting the live
  `.projects-list` / `.content-card` markup. Superseded by 1.3.7 (hidden block +
  visible spinner), which removes the flash entirely.

Generator and Templates only; Framework unchanged at 1.3.5.

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
