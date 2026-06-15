# WitDocs — Work Plan

Живой трекер работ по фреймворку и сайтам на его основе. Отмечаем прогресс по ходу.
Создан на основе глубокого аудита (см. историю обсуждения). Дата старта: 2026-06-15.

Статусы: ⬜ не начато · 🟡 в работе · ✅ готово · ⏸ отложено

---

## Контекст: 4 направления

1. Исправить найденные в аудите проблемы (фреймворк, генератор, сборка, CI, тесты).
2. Вынести/обобщить модель расширения markdown-компонентов (плагины без правки фреймворка).
3. Привести 4 сайта к единому виду: ratner.io, witengine.io, witdatabase.io, witrpc.io.
4. Починить проблему с краулерами (роботы не могут читать сайты).

### Ключевой диагноз по п.4 (подтверждён live-curl на ratner.io 2026-06-15)
Проблема **двойная**:
- **4a — Cloudflare блокирует AI-ботов (НЕ баг кода).** GPTBot/ClaudeBot → HTTP **403 "Your request was blocked"** на краю Cloudflare. Googlebot/Bingbot/facebookexternalhit/браузер → 200. Причина: включена функция Cloudflare **"Block AI Scrapers and Crawlers"** (или Bot Fight Mode / WAF). Чинится в дэшборде Cloudflare.
- **4b — SSG не пререндерит главную (код).** Googlebot на `/` получает пустую SPA-оболочку ("Loading…"). `StaticPageGenerator` генерит blog/contact/search + детальные, но НЕ `/` и не list-страницы; плюс `AppDivRegex` ломается о вложенный `<div class="loading-indicator">` в реальном index.html.

---

## Phase 0 — 4a: Разблокировать AI-краулеры (ops, действие пользователя)
> Делается параллельно с Phase 1. Требует доступа к Cloudflare дэшборду.
> Два переключателя в Security → Bots на каждую зону:
> 1. **Block AI bots → «Do not block (allow crawlers)»**
> 2. **Manage your robots.txt → «Disable robots.txt configuration»**

- ✅ **ratner.io** — оба выключены. Verify: GPTBot/ClaudeBot/OAI-SearchBot/PerplexityBot → **200**; robots.txt чистый (`Allow: /`); detail-страница отдаёт полный текст + OG. (2026-06-15)
- ⬜ **witengine.io** — повторить оба переключателя + verify.
- ⬜ **witdatabase.io** — повторить оба переключателя + verify.
- ⬜ **witrpc.io** — повторить оба переключателя + verify.
- ℹ️ AI Labyrinth / Bot Fight Mode — оставить Off. Cloudflare managed ruleset — Always active, не трогать.

---

## Phase 1 — Фундамент фреймворка (п.1 + 4b)

### 1.1 SSG / SEO (приоритет — напрямую чинит то, что видит Googlebot) ✅
- ✅ Пререндер главной страницы `/` — h1 + список проектов и последних постов со ссылками (StaticPageGenerator.GenerateHomePageAsync).
- ✅ Пререндер list-страниц секций (blog/articles/docs/dynamic) с реальными ссылками; contact/search — всегда; пустые секции пропускаются.
- ✅ Заменён хрупкий `AppDivRegex` на устойчивую замену через подсчёт вложенности div (`ReplaceAppContent`). H8. Покрыто тестом `ReplaceAppContentHandlesNestedDivsTest`.
- ✅ Единый Markdig-пайплайн: Generator переиспользует `MarkdownService` (auto-id якоря, task lists, emoji, frontmatter). H4/H5.
- ✅ Summary в списках чистятся (`ExtractPlainText`) и обрезаются (200). Проверено на реальном контенте ratner.io (19 стат. страниц, 0 orphaned div).
- ⬜ Осталось из H5: убрать дубль `RenderInlineMarkdown` в ContentMetadataGenerator (вернуть guard или перейти на MarkdownService).

### 1.2 Сборка / CI / упаковка
- ✅ **C1:** шаблон возит `.config/dotnet-tools.json` (пин 1.3.4); targets делают авто-`dotnet tool restore` при наличии манифеста + понятный `<Error>` вместо голого MSB3073. Обратно совместимо (сайты без манифеста → глобальный тул). Проверено: restore+вызов из манифеста, exit 0.
- ✅ **C2:** `test.yml` → `OutWit.slnx` (был несуществующий `OutWit.sln`) + добавлен Generator в выбор. Коммит 0552282.
- ✅ **H9:** опечатка `$HAS_SN_UPKG` → `$HAS_SNUPKG`; тест-гейт на publish/pack. Коммит 0552282.
- ✅ **H10:** добавлен `.gitignore` в шаблон (его не было) — игнорит сгенерированные SEO/контент-артефакты и hosting-конфиги. Чистое дерево у новых юзеров, нет пустых плейсхолдеров в проде.
- ⬜ Централизовать версии (`Directory.Build.props`/CPM); убрать дубль copy-target. **ОТЛОЖЕНО:** `Directory.Build.props` имеет незакоммиченную правку пользователя (Product→WitDocs) — не смешиваю.

### 1.3 Безопасность
- ✅ **C3:** флаг `SiteConfig.AllowRawHtml` (default true). `MarkdownService(bool)` + `Configure()`; при false → `.DisableHtml()` (без зависимостей, 0 кБ payload). Генератор берёт флаг синхронно из SiteConfig; рантайм — через `ConfigService` после загрузки config. 3 теста. Решение пользователя: вариант «опция-флаг».
- ✅ **H7:** YouTube-iframe — `Uri.EscapeDataString` для id, `HtmlEncode` для Title.
- ✅ **H6:** XML-escaping в sitemap (`<loc>`) и RSS (`<link>`/`<guid>`/atom href).
- ✅ JSON-LD: `<` → `<` (закрывает `</script>`-выход), canonical через escaper.

### 1.4 Корректность / надёжность
- ✅ **C4:** генератор ловит фатал, печатает понятную ошибку, `return 1` (раньше всегда `return 0`).
- ✅ **H1:** lock + double-check в `GetDocsAsync`; кэш публикуется только после полной сборки.
- ✅ **H2:** убрано поле `m_placeholderCounter`, локальный счётчик (singleton `ContentParser` потокобезопасен).
- ⬜ **H3:** параллельная загрузка (`Task.WhenAll` fetch + sequential parse) в `Get*Async` — отдельный согласованный проход (перф, не корректность).
- ⬜ Не глотать молча ошибки YAML/site.config.json (логировать).
- ⬜ Убрать двойной рендеринг markdown в `ParseWithFrontmatter`.

### 1.5 Код-стайл / рефактор (low, по возможности)
- ✅ `ThemeMode.cs`: 4-space, `static readonly`.
- ⬜ Разбить `ContentService.cs` (991 строк).
- ⬜ Дедуп: TOC (TocItem/TocEntry), Parse*, GetBasePathFor*, JsonIndexService<T>.

### 1.6 Тесты
- ⬜ ContentService/SearchService (fake HttpMessageHandler).
- ⬜ Интеграционный тест `GenerateAllAsync` (полный пайплайн → проверка артефактов).
- ⬜ Починить нейминг SlugGeneratorTests; NavigationServiceTests тестирует модели.
- ⬜ Тест на корректность SSG-вывода (валидный HTML, нет orphaned div).

---

## Phase 2 — Модель расширения markdown-компонентов (п.2)
- ⬜ Спроектировать публичный API регистрации кастомного компонента в DI (без правки фреймворка).
- ⬜ Динамический рендер по типу в рантайме (`EmbeddedComponentRenderer`/`ContentWithComponents`).
- ⬜ Поддержка кастомных компонентов в SSG-генераторе (рендер или корректная деградация).
- ⬜ Документация + пример нового компонента.

---

## Phase 3 — Ревью и унификация 4 сайтов (п.3)
- ⬜ ratner.io — привести к актуальному фреймворку, redeploy, verify.
- ⬜ witengine.io — то же.
- ⬜ witdatabase.io — то же.
- ⬜ witrpc.io — то же.
- ⬜ Унифицировать: index.html, deploy.yml, версии пакетов, структуру.
- ⬜ End-to-end проверка на каждом: краулеры (curl UA), OG-теги, sitemap, прямой fetch detail-страниц.

---

## Лог решений
- 2026-06-15: Старт. Выбрано: Phase 1 кодит ассистент, 4a — пользователь параллельно. Трекер в этом файле.
