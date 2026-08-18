# Changelog

All notable changes to the WitDocs packages (OutWit.Docs.Framework,
OutWit.Docs.Generator, OutWit.Docs.Templates) are documented here.

## 2.4.1

### Fix (Generator — SSG): every local build nested another copy of the home page

- **Bug:** the template is read from the output directory's own `index.html`, and
  the home page is written back to that same file. The loading indicator was
  taken from the template's `<div id="app">` verbatim, so on the second run the
  indicator *was* the first run's entire injection, and each run nested another
  copy of the home page inside the last.

  Found on witdatabase.io: after eleven local `dotnet build -c Release` runs its
  `index.html` had grown from 2.6KB to **45.6KB with eleven nested copies** of
  the home page, and was one `git add` away from being committed. CI was never
  affected — it builds from a clean checkout, so it always ran exactly once.

- **Fix:** the indicator is now recovered rather than copied. Every injection
  keeps the original in its `.ssg-loading` div, so unwrapping that as many times
  as it has been wrapped recovers the template's own markup however many times
  the generator has already run over the file. It is trimmed as well, because the
  injection puts a newline either side and otherwise each run would keep the
  previous run's whitespace.

- **Generating twice now produces the same file, byte for byte** — asserted by a
  test, along with one that runs four times and checks a single `ssg-prerender`
  block survives, and one that starts from an already-nested `index.html` and
  checks the stale copies are dropped rather than added to.

- **For a site that already has a nested `index.html`:** running 2.4.1 over it
  repairs it. Nothing to undo by hand.

### Fix (Templates): a site created from the template could not restore its tool

- **Bug:** `templates/witdocs/.config/dotnet-tools.json` pinned
  `outwit.docs.generator` at **1.4.4**, a version that does not exist under that
  package id — 1.4.4 was the last of the pre-rename `outwit.web.generator`. Every
  project created from the template therefore failed `dotnet tool restore`, and
  the manifest the framework's error message points people at was the one thing
  that could not work.
- Pinned to **2.4.1**. `dotnet new witdocs` → `dotnet tool restore` →
  `dotnet build -c Release` now runs through, generation included.

### Templates: versions brought up to date

| Reference | Was | Now |
|---|---|---|
| `outwit.docs.generator` (tool manifest) | 1.4.4 | 2.4.1 |
| `OutWit.Docs.Framework` | 2.0.0 | 2.4.0 |
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.1 | 10.0.11 |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | 10.0.1 | 10.0.11 |
| Package version | 2.0.0 | 2.4.1 |

- The template's `.gitignore` gains the prerendered pages — `wwwroot/*/index.html`
  and `wwwroot/*/*/index.html`. It already covered the generated indexes, the
  hosting config and the OG images; the pages themselves were the gap, and they
  are what would otherwise be committed by a new site.

## 2.4.0

### Feature (Framework — content): an image in markdown can be opened at its own size

- **Why:** a documentation screenshot is wider than the column it is shown in.
  witdatabase.io ships 53 frames captured at 1440px and displayed in an article
  column of roughly 740px, so every one of them is a thumbnail whether or not it
  was meant to be one, and the UI text inside is half the size it was taken at.
  The reader had no way to get to the original short of opening the file by hand.
- **What:** a paragraph holding nothing but an image is now rendered as a
  `<figure>` whose picture opens full size in an overlay when clicked. Nothing
  changes in the markdown: `![alt](/images/shot.webp)` is enough.

  ```html
  <figure class="ow-figure">
      <button type="button" class="ow-figure__zoom" aria-label="Open the image at full size: …">
          <img src="…" alt="…" loading="lazy" decoding="async" />
      </button>
      <figcaption class="ow-figure__caption">…</figcaption>
  </figure>
  ```

- **Captions are opt-in, per image**, through the title markdown already has:

  ```markdown
  ![alt text](/images/shot.webp "What the picture shows")
  ```

  With no title there is no `<figcaption>`, so no existing site changes
  appearance. The alt text stays where it belongs, on the image, and is also
  used as the accessible name of the zoom button.

- **Click, not hover.** Hover fires while a reader is scrolling past, cannot be
  dismissed deliberately, does not exist on a touchscreen, and gives no way to
  study the picture once it is up. The overlay closes on `Escape`, on a click
  anywhere outside the picture, and on its own close button; focus returns to the
  image that was clicked.

- **What is not a figure**, because turning these into blocks would break the
  page around them:

  | Markdown | Rendered as |
  |---|---|
  | An image alone in a paragraph | `<figure>`, zoomable |
  | An image in a sentence — a badge, an icon | inline `<img>`, untouched |
  | A linked image `[![…](…)](url)` | the link the author wrote |
  | Two images in one paragraph | two inline `<img>` |
  | An image in a tight list item | inline `<img>` |

- **Implementation.** `ImageZoomExtension` replaces Markdig's `ParagraphRenderer`
  (`Services/MarkdownImageZoom.cs`), the same shape as the existing code-block
  extension. The markup is static, so the generated pages carry the figure and
  the caption for crawlers, and one delegated listener in `framework.js` — beside
  the one for the code copy button — builds the overlay lazily on first use. No
  per-image interop, no component to write, nothing to opt into.
- **Styles** land in `outwit-framework.css`, which had no `img` rules at all
  before this: `.ow-figure`, `.ow-figure__zoom`, `.ow-figure__caption`,
  `.ow-lightbox`. They use the existing tokens, including the `--z-modal` that
  had been reserved and unused. A site that already styles `.prose img` keeps
  winning, since its own stylesheet loads later.
- One of those rules is load-bearing and easy to lose: `.ow-lightbox` is laid out
  with `display: flex`, which outranks the browser's own `[hidden] { display: none }`,
  so without `.ow-lightbox[hidden] { display: none }` the *closed* overlay stays over
  the whole page at opacity 0 and swallows every click. Caught on witdatabase.io,
  where the nav stopped responding after closing a picture. `StylesheetTests` now
  asserts that rule and three others against the shipped sheet.

### Fix (Framework — build): `dotnet build` failed wherever the generator was installed globally

- **Bug:** the content-generation target ran `dotnet witdocs-generate`. That form
  only ever resolves a tool from a local manifest; a globally installed one lives
  on PATH under its own name. No site repository carries a
  `.config/dotnet-tools.json`, so `dotnet build` failed on every one of them with
  "The 'witdocs-generate' tool is missing or errored" even though the tool was
  installed and worked when invoked directly. CI was unaffected, because the
  workflow calls `witdocs-generate` itself.
- **Fix:** when the manifest call fails, the target now retries with the global
  name before reporting the error. Both paths keep their exit code, so a genuine
  generator failure still stops the build with the same message.

### Generator 2.4.0

- Rebuilt against Framework 2.4.0, so generated pages carry the new figure markup.
  No behaviour of its own changed.

## 2.3.4

### Fix (Framework — header): the row overflowed instead of giving way

- **Bug:** nothing in the header row could shrink. The links do not wrap and are
  `white-space: nowrap`, the search field is a fixed 140px, and the only
  responsive rule was the mobile breakpoint at 768px. A site with eight
  top-level entries therefore ran out of room well above that and pushed the
  whole document into a horizontal scroll, clipping the search field and taking
  the theme toggle off-screen with it. Every window between 768px and roughly
  1250px was affected, which covers a maximised 1024x768 laptop and any
  half-screen window on a 1080p display. Found on witdatabase.io.
- **Fix:** the row gives way in stages instead: the container gap, link padding,
  link size and search field tighten, then the search is dropped, then the
  mobile menu takes over.
- **New optional `header.collapseBreakpoint` in `site.config.json`.** How much
  room the row needs depends on the menu, so the width it collapses at is a
  per-site number rather than one constant for everybody. Eight entries with
  dropdowns run out of room around 1000px; three short ones hold on past 700px.

  ```json
  "header": { "collapseBreakpoint": 1000 }
  ```

  Default **1000**, clamped to 480..1600. The other two steps are derived from
  it, so one number configures the whole ladder: the row tightens 300px above it
  and the search is dropped 150px above it. The `Header` component writes the
  rules out, because a media query cannot read a custom property. `SiteConfig`
  gains a `Header` property (`HeaderConfig`).
- **Behaviour change:** the nav now hands over to the burger at the configured
  width, 1000px by default, rather than at a fixed 768px. A site with a short
  menu that wants the old behaviour sets `collapseBreakpoint` to 768. The 768px
  rule stays in the stylesheet as a floor, so a phone always gets the mobile
  menu whatever the site configures.
- `header__container` in `outwit-framework.css` had a fixed `height: 4rem`, which
  clipped anything taller than one row; it is now `min-height`.
- `.header__mobile-menu` is revealed at the new breakpoint too. Its visibility
  lived only in `outwit-framework.css` at 768px, so between 768 and 1000 the
  toggle would have opened nothing.

### Fix (Framework — header): the mobile menu was one flat list

- `header__mobile-link--sub` has always been emitted for a section's pages and
  was styled nowhere, so a section and the pages under it were indistinguishable.
  Sub-entries are now indented and set slightly smaller and quieter.
- Sub-entries also used the old prefix match for their highlight, so the 2.3.3
  fix reached the desktop dropdown but not the mobile menu, where a section, its
  overview and the current page all lit up together. They now use the same
  `IsActiveChild` rule.

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
