# Changelog

All notable changes to the WitDocs packages (OutWit.Docs.Framework,
OutWit.Docs.Generator, OutWit.Docs.Templates) are documented here.

## 2.3.3

### Fix (Framework — header): two menu entries highlighted at once

- **Bug:** `IsActive` asked whether the current path *starts with* the link's href,
  so inside a dropdown every ancestor entry lit up alongside the current page. A
  landing section (2.1.0) makes this the normal case: its lead article is served at
  the bare section route, so an "Overview" entry pointing at `/quick-start` matched
  `/quick-start/first-database` and both went green. Found on witdatabase.io.
- **Fix:** dropdown entries now highlight only the **most specific** matching
  sibling. `Header.razor` calls the new `IsActiveChild(parent, child)`, which keeps
  an entry active unless a sibling with a longer matching href describes the page
  better. Top-level items are unchanged and still highlight for everything beneath
  them, so `/blog` stays lit on `/blog/some-post`.
- Matching also became segment-aware and tolerant of the URL's tail: `/blog` no
  longer matches `/blogroll`, trailing slashes on either side are ignored, and a
  query string or fragment (`/search?query=x`) no longer has to be part of the
  prefix. The root is active only on the root, as before.
- `HeaderViewModel` gains `IsActiveChild`, plus `IsMatch` and `IsMostSpecificMatch`
  as internal statics with test coverage. No configuration changes; sites pick the
  fix up by upgrading the package.

- The generator was unchanged in this release and stays at 2.3.2.

## 2.3.2

### Cross-domain canonical URLs (Framework + Generator)

- **New optional `canonicalUrl` in frontmatter.** When the same text is published on
  more than one site, the secondary copy can now point at the primary one:

  ```yaml
  ---
  title: 'Comparing RPC Frameworks in .NET Applications'
  canonicalUrl: 'https://ratner.io/article/comparing-rpc-frameworks-in-dotnet/'
  ---
  ```

  Without it nothing changes — the canonical stays the page's own URL.
- Supported on **blog posts** and **articles** (including dynamic sections, which use
  the article path); those are the types that get syndicated. `BlogPost` and
  `ArticleCard` gain a `CanonicalUrl` property, and `FrontmatterData` carries it.
- Applies to both renderers, so the prerendered HTML and the hydrated SPA agree:
  `BlogPostPage` and `ArticlePage` pass it to `SeoHead`, and `StaticPageGenerator`
  uses it instead of the computed URL for content and section pages.
- **Known limitation:** `sitemap.xml` still lists the page under its own URL. A
  sitemap entry for a page that declares a different canonical is a soft
  inconsistency rather than an error — search engines treat `rel="canonical"` as the
  stronger signal — but `SitemapGenerator` builds URLs from the content index without
  opening the files, so honouring the override there is a larger change and is not
  part of this release.

## 2.3.1

### Fix (Generator — hosting config): stylesheets were cached immutably

- **Bug:** `/css/*` was emitted as `public, max-age=31536000, immutable` for
  Cloudflare, Netlify and Vercel, but a site's stylesheets (`css/site.css`,
  `css/theme.css`) carry **no content hash** in their names. `immutable` tells the
  browser never to revalidate, so anyone who had loaded the site before a style
  change kept the old stylesheet against the new markup — for up to a year — while
  a first-time visitor saw the site correctly. Found on witrpc.io after a redesign:
  the hero's code block rendered centred because the rule that left-aligns it did
  not exist in the cached css.
- **Fix:** `/css/*` is now `no-cache` (stored, but revalidated — a 304 when
  unchanged), the same treatment the boot loaders got in 1.4.2–1.4.4 for exactly
  this reason. `immutable` now applies only to content-hashed assets
  (`_framework/*.wasm`, `_framework/*.dat`) and `/images/*` keeps its bounded
  `max-age=86400`.

### Fix (Generator — hosting config): Vercel pinned the boot loaders too

- The stale-app fix of 1.4.2–1.4.4 reshaped the Cloudflare and Netlify rules but
  left `vercel.json` with a single broad `/_framework/(.*)` marked immutable, which
  also covers the stable-named `dotnet.js` and `blazor.webassembly.js` — the exact
  files that release was about. Vercel now follows the Cloudflare shape: the two
  boot loaders are `no-cache` and hashed assets are marked by extension
  (`*.wasm`, `*.dat`), so no broad rule exists and rule precedence never matters.

- Framework was unchanged in this release and stayed at 2.3.0.

> After upgrading the generator and redeploying, purge the CDN cache once so the
> edge stops serving the previously-immutable stylesheet. Returning visitors whose
> browser already cached the old css will keep it until its year expires unless the
> site also changes the stylesheet URL (e.g. `css/site.css?v=2`) once.

## 2.3.0

### Analytics — opt-in tracker snippet injection (Generator + Framework)

- **New opt-in `analytics` section in `site.config.json`.** When `scriptUrl` and
  `websiteId` are both set, the generator injects a provider-agnostic tracker
  snippet before `</head>` of every prerendered page (including the root
  `index.html`):
  `<script defer src="{scriptUrl}" data-website-id="{websiteId}" ...></script>`.
- Optional `domains` (rendered as `data-domains` — drops events from
  localhost/previews client-side) and `excludeSearch` (rendered as
  `data-exclude-search`, **default true** — path-only tracking, query strings never
  leave the browser).
- The contract is deliberately minimal (script URL + website id + data attributes),
  so the analytics backend can be swapped — e.g. self-hosted Umami today, another
  collector tomorrow — by changing two config values, or nothing at all if the
  backend keeps the same script path.
- When the section is absent or incomplete the feature is a no-op — nothing is
  injected, zero payload cost.
- `SiteConfig` gains an `Analytics` property (`AnalyticsConfig`).

## 2.2.2

### IndexNow — opt-in verification key file (Generator + Framework)

- **New opt-in SEO feature.** Set `seo.indexNowKey` in `site.config.json` to a public
  IndexNow key (a–z, A–Z, 0–9, dash; 8–128 chars) and the generator writes the required
  `{key}.txt` verification file to the site root next to `sitemap.xml`/`robots.txt`.
  When the key is unset the feature is a no-op — nothing is emitted.
- The generated `sitemap.xml` is the URL source for the deploy pipeline's IndexNow
  submission step (Bing, Yandex, Seznam, Naver participate; Google does not).
- `SeoConfig` gains an `IndexNowKey` property (nullable, off by default).

## 2.2.1

### OG images — fix content-page `og:image` for singular routes (Generator + Framework)

- **Bug:** content pages in a section whose route is singular but whose folder is
  plural (`projects` → `/project/{slug}`, `articles` → `/article/{slug}`) pointed
  `og:image` at `project-{slug}.png` / `article-{slug}.png`, but `OgImageGenerator`
  names the file after the folder — `projects-{slug}.png` / `articles-{slug}.png`.
  The referenced image did not exist, so social share previews (Facebook, LinkedIn,
  Slack, WhatsApp, Twitter) showed a broken/empty image. `blog` and `docs` were
  unaffected (their route prefix equals the folder name). This is the two-segment
  companion to the 2.2.0 landing-page fix.
- **Generator:** `StaticPageGenerator` now emits an explicit `og:image` override
  (`/og-images/{folder}-{slug}.png`) for content pages instead of auto-detecting it
  from the singular canonical URL.
- **Framework:** `SeoHeadViewModel` maps the singular route prefix to the plural
  folder name (`project` → `projects`, `article` → `articles`) when resolving the
  runtime `og:image`.

## 2.2.0

### OG images — follow the site's default theme + per-landing image (Generator)
- OG images now render in the site's **default theme** instead of always dark:
  `defaultTheme: "light"` → light background, dark text, the `logo-light` artwork;
  `dark` (default) → the previous dark treatment. Colors (background, accent, text)
  are read from the matching `theme.css` scope (`:root` vs `[data-theme="dark"]`);
  the OG HTML template's text colors are now themed (`{{TEXT_COLOR}}`/`{{DESC_COLOR}}`/
  `{{URL_COLOR}}`).
- **Landing pages get their own OG image.** A section lead page served at the short
  route (`/{route}/`, see 2.1.0) now references its generated
  `og-images/{section}-{slug}.png` instead of falling back to the default image (its
  single-segment canonical URL previously defeated the URL-based lookup).

### Footer — Reddit social icon

- `SocialIcon` now renders the official **Reddit** logo for `platform: "reddit"`
  (footer `socialLinks`). Previously unknown platforms (incl. reddit) fell back to
  the generic globe icon. Other platforms unchanged; unknown ones still fall back
  to the globe (handy for a generic "portal"/website link).

## 2.1.0

### Sections — short "landing" URLs (opt-in)

- New `ContentSectionConfig.LandingPage` (bool, default `false`). When `true`, a
  dynamic section's **lead (first) page** is served at the short section route
  itself (`/{route}/`) instead of `/{route}/{lead-slug}/`, and no card-listing
  page is generated for the root; the remaining pages keep `/{route}/{slug}/`.
  Each page then has exactly **one canonical URL**, so the human-navigable paths
  match the sitemap/search-index. Ordering uses the usual `NN-` filename prefixes
  (the lead is the lowest-ordered file).
- Applies across the generator: static HTML (`StaticPageGenerator`), `sitemap.xml`
  (`SitemapGenerator`) and `search-index.json` (`SearchIndexGenerator`) all emit
  the canonical `/{route}/` for the lead and `/{route}/{slug}/` for the rest.
- Backward compatible: sections without `landingPage` keep the previous behavior
  (listing root + every page under `/{route}/{slug}/`). No change for existing
  sites until they opt in.
- Tip: give the lead page `showInMenu: false` so the section's auto dropdown lists
  only the sub-pages (the lead is reached via the top-level menu item itself).

> Publish **OutWit.Docs.Framework** then **OutWit.Docs.Generator** 2.1.0 (the
> generator tool depends on the framework package); consumer sites pick up the new
> URLs on their next generate/deploy.

## 2.0.0

### Breaking — rebrand OutWit.Web → OutWit.Docs (WitDocs)

- **Package IDs renamed**: `OutWit.Web.Framework` → `OutWit.Docs.Framework`,
  `OutWit.Web.Generator` → `OutWit.Docs.Generator`, `OutWit.Web.Templates` →
  `OutWit.Docs.Templates`. The old `OutWit.Web.*` packages remain published (1.x) but
  are deprecated; migrate `PackageReference`s to `OutWit.Docs.*`.
- **CLI tool command** renamed `outwit-generate` → `witdocs-generate`
  (tool package `OutWit.Docs.Generator`, manifest id `outwit.docs.generator`).
- **Template** short name `outwit-web` → `witdocs` (`dotnet new witdocs`).
- Root namespaces/assemblies moved `OutWit.Web.*` → `OutWit.Docs.*`; the framework
  build props file is now `build/OutWit.Docs.Framework.targets`.
- **Unchanged on purpose:** the MSBuild property surface (`OutWitHostingProvider`,
  `OutWitGenerateContent`, …) keeps its `OutWit*` prefix, so consumer csproj/CI need
  no property changes — only the `PackageReference`, tool install, and `@using`.

## 1.4.4

### Fixes (Generator — hosting config)
- Definitive stale-app fix. Cloudflare `_headers` does **not** let a specific rule
  override a wildcard for the same header (regardless of order — confirmed on
  prod), so any `/_framework/* immutable` rule re-pinned the boot loaders.
  Removed the broad wildcard: hashed assets are now cached `immutable` by
  extension (`*.wasm`, `*.dat`), and `dotnet.js` / `blazor.webassembly.js` are
  `no-cache` with no conflicting rule. (Supersedes 1.4.2/1.4.3.)

## 1.4.3

### Fixes (Generator — hosting config)
- Attempted ordering fix (specific rules before the wildcard). Cloudflare ignored
  it — superseded by 1.4.4, which drops the conflicting wildcard entirely.

> After upgrading the generator and redeploying, purge the CDN cache once to
> evict the previously-immutable boot files.

## 1.4.2

### Fixes (Generator — hosting config)
- Stop marking the stable-named Blazor boot entry points (`dotnet.js`,
  `blazor.webassembly.js`) as `immutable`; they change every deploy, and the
  year-long immutable cache pinned the SPA to a stale build. (Superseded by 1.4.3,
  which fixes the rule ordering so this actually applies.)

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
  so `dotnet new witdocs` + `dotnet build -c Release` works without a global
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
