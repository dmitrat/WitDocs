using System.Net.Http.Json;
using System.Text.RegularExpressions;
using OutWit.Web.Framework.Content;
using OutWit.Web.Framework.Models;
using OutWit.Web.Framework.Utils;

namespace OutWit.Web.Framework.Services;

/// <summary>
/// Service for loading content from markdown files in wwwroot/content folder.
/// Enables dynamic content loading without hardcoding - just add .md files to the content folder.
/// </summary>
public partial class ContentService
{
    #region Fields

    // Cache for loaded content
    private List<BlogPost>? m_blogPosts;
    private List<ProjectCard>? m_projects;
    private List<ArticleCard>? m_articles;
    private List<FeatureCard>? m_features;
    private Dictionary<string, DocPage>? m_docs;
    private ContentIndex? m_contentIndex;
    
    // Async locks for thread-safe caching
    private readonly SemaphoreSlim m_projectsLock = new(1, 1);
    private readonly SemaphoreSlim m_blogPostsLock = new(1, 1);
    private readonly SemaphoreSlim m_articlesLock = new(1, 1);
    private readonly SemaphoreSlim m_featuresLock = new(1, 1);

    #endregion

    #region Constructors

    public ContentService(HttpClient http, MarkdownService markdownService, ContentParser contentParser)
    {
        Http = http;
        MarkdownService = markdownService;
        ContentParser = contentParser;
    }

    #endregion

    #region Blog

    /// <summary>
    /// Get all blog posts sorted by date (newest first).
    /// </summary>
    public async Task<List<BlogPost>> GetBlogPostsAsync()
    {
        if (m_blogPosts != null)
            return m_blogPosts;

        // Prevent race condition with lock
        await m_blogPostsLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (m_blogPosts != null)
                return m_blogPosts;

            var index = await GetContentIndexAsync();
            var posts = new List<BlogPost>();

            foreach (var file in index.Blog)
            {
                try
                {
                    var markdown = await Http.GetStringAsync($"content/blog/{file}");
                    var post = ParseBlogPost(file, markdown);
                    if (post != null)
                        posts.Add(post);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading blog post {file}: {ex.Message}");
                }
            }

            // Deduplicate by slug and sort by date
            m_blogPosts = posts
                .GroupBy(p => p.Slug)
                .Select(g => g.First())
                .OrderByDescending(p => p.PublishDate)
                .ToList();
                
            return m_blogPosts;
        }
        finally
        {
            m_blogPostsLock.Release();
        }
    }

    /// <summary>
    /// Get a specific blog post by slug.
    /// Uses cached data if available, otherwise loads directly from file.
    /// </summary>
    public async Task<BlogPost?> GetBlogPostAsync(string slug)
    {
        // Check cache first
        if (m_blogPosts != null)
        {
            return m_blogPosts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        // Direct load: find and load only the requested file
        return await GetBlogPostDirectAsync(slug);
    }

    /// <summary>
    /// Load a specific blog post directly by slug without loading all posts.
    /// This is the fast path for single post pages.
    /// </summary>
    public async Task<BlogPost?> GetBlogPostDirectAsync(string slug)
    {
        try
        {
            var index = await GetContentIndexAsync();
            
            // Find the file that matches the slug
            var matchingFile = index.Blog.FirstOrDefault(file => 
            {
                var fileSlug = SlugGenerator.GetSlugFromFilename(file);
                return fileSlug.Equals(slug, StringComparison.OrdinalIgnoreCase);
            });

            if (matchingFile == null)
                return null;

            var markdown = await Http.GetStringAsync($"content/blog/{matchingFile}");
            return ParseBlogPost(matchingFile, markdown);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading blog post {slug}: {ex.Message}");
            return null;
        }
    }
    
    private BlogPost? ParseBlogPost(string filename, string markdown)
    {
        var (frontmatter, _) = MarkdownService.ParseWithFrontmatter<FrontmatterData>(markdown);
        if (frontmatter == null) return null;

        var slug = SlugGenerator.GetSlugFromFilename(filename);
        var content = ExtractContentWithoutFrontmatter(markdown);

        // Extract embedded components [[Component ...]] and get processed content
        var (processedContent, components) = ContentParser.Transform(content);

        // Set base path for relative URL resolution
        var basePath = GetBasePathForBlogPost(filename);
        foreach (var component in components)
        {
            component.BasePath = basePath;
        }

        // Render processed markdown (with component placeholders) to HTML
        var html = MarkdownService.ToHtml(processedContent);

        return new BlogPost
        {
            Slug = slug,
            Title = frontmatter.Title ?? slug,
            Description = !string.IsNullOrEmpty(frontmatter.Description)
                ? MarkdownService.ToHtmlInline(frontmatter.Description)
                : "",
            Summary = !string.IsNullOrEmpty(frontmatter.Summary)
                ? MarkdownService.ToHtmlInline(frontmatter.Summary)
                : "",
            PublishDate = frontmatter.PublishDate,
            Tags = frontmatter.Tags ?? new List<string>(),
            FeaturedImage = frontmatter.FeaturedImage ?? "",
            Author = frontmatter.Author ?? "",
            RawContent = content,
            HtmlContent = html,
            ReadingTimeMinutes = MarkdownService.CalculateReadingTime(content),
            EmbeddedComponents = components
        };
    }

    #endregion

    #region Project

    /// <summary>
    /// Get all projects sorted by order (from filename prefix).
    /// </summary>
    public async Task<List<ProjectCard>> GetProjectsAsync()
    {
        if (m_projects != null)
            return m_projects;

        // Prevent race condition with lock
        await m_projectsLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (m_projects != null)
                return m_projects;

            var index = await GetContentIndexAsync();
            var projects = new List<ProjectCard>();

            foreach (var file in index.Projects)
            {
                try
                {
                    var markdown = await Http.GetStringAsync($"content/projects/{file}");
                    var project = ParseProject(file, markdown);
                    if (project != null)
                        projects.Add(project);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading project {file}: {ex.Message}");
                }
            }

            // Deduplicate by slug and sort by order
            m_projects = projects
                .GroupBy(p => p.Slug)
                .Select(g => g.First())
                .OrderBy(p => p.Order)
                .ToList();
                
            return m_projects;
        }
        finally
        {
            m_projectsLock.Release();
        }
    }

    /// <summary>
    /// Get a specific project by slug.
    /// Uses cached data if available, otherwise loads directly from file.
    /// </summary>
    public async Task<ProjectCard?> GetProjectAsync(string slug)
    {
        // Check cache first
        if (m_projects != null)
        {
            return m_projects.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        // Direct load: find and load only the requested file
        return await GetProjectDirectAsync(slug);
    }

    /// <summary>
    /// Load a specific project directly by slug without loading all projects.
    /// This is the fast path for single project pages.
    /// </summary>
    public async Task<ProjectCard?> GetProjectDirectAsync(string slug)
    {
        try
        {
            var index = await GetContentIndexAsync();
            
            // Find the file that matches the slug
            var matchingFile = index.Projects.FirstOrDefault(file => 
            {
                var (_, fileSlug) = SlugGenerator.GetOrderAndSlugFromFilename(file);
                return fileSlug.Equals(slug, StringComparison.OrdinalIgnoreCase);
            });

            if (matchingFile == null)
                return null;

            var markdown = await Http.GetStringAsync($"content/projects/{matchingFile}");
            return ParseProject(matchingFile, markdown);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading project {slug}: {ex.Message}");
            return null;
        }
    }
    
    private ProjectCard? ParseProject(string filename, string markdown)
    {
        var (frontmatter, _) = MarkdownService.ParseWithFrontmatter<FrontmatterData>(markdown);
        if (frontmatter == null) return null;

        var (order, slug) = SlugGenerator.GetOrderAndSlugFromFilename(filename);
        var content = ExtractContentWithoutFrontmatter(markdown);
        
        // Extract embedded components [[Component ...]] and get processed content
        var (processedContent, components) = ContentParser.Transform(content);
        
        // Set base path for relative URL resolution (for folder-based content)
        var basePath = GetBasePathFromFilename(filename);
        foreach (var component in components)
        {
            component.BasePath = basePath;
        }
        
        // Render processed markdown (with component placeholders) to HTML
        var html = MarkdownService.ToHtml(processedContent);

        return new ProjectCard
        {
            Slug = slug,
            Order = order,
            Title = frontmatter.Title ?? slug,
            Description = !string.IsNullOrEmpty(frontmatter.Description)
                ? MarkdownService.ToHtmlInline(frontmatter.Description)
                : "",
            Summary = !string.IsNullOrEmpty(frontmatter.Summary)
                ? MarkdownService.ToHtmlInline(frontmatter.Summary)
                : "",
            Tags = frontmatter.Tags ?? new List<string>(),
            Url = frontmatter.Url ?? "",
            MenuTitle = frontmatter.MenuTitle ?? "",
            ShowInMenu = frontmatter.ShowInMenu,
            ShowInHeader = frontmatter.ShowInHeader,
            IsFirstProject = frontmatter.IsFirstProject,
            RawContent = content,
            HtmlContent = html,
            EmbeddedComponents = components
        };
    }


    #endregion

    #region Articles

    /// <summary>
    /// Get all articles sorted by order (from filename prefix).
    /// </summary>
    public async Task<List<ArticleCard>> GetArticlesAsync()
    {
        if (m_articles != null)
            return m_articles;

        await m_articlesLock.WaitAsync();
        try
        {
            if (m_articles != null)
                return m_articles;

            var index = await GetContentIndexAsync();
            var articles = new List<ArticleCard>();

            foreach (var file in index.Articles)
            {
                try
                {
                    var markdown = await Http.GetStringAsync($"content/articles/{file}");
                    var article = ParseArticle(file, markdown, "articles");
                    if (article != null)
                        articles.Add(article);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading article {file}: {ex.Message}");
                }
            }

            m_articles = articles
                .GroupBy(a => a.Slug)
                .Select(g => g.First())
                .OrderBy(a => a.Order)
                .ToList();

            return m_articles;
        }
        finally
        {
            m_articlesLock.Release();
        }
    }

    /// <summary>
    /// Get a specific article by slug.
    /// Uses cached data if available, otherwise loads directly from file.
    /// </summary>
    public async Task<ArticleCard?> GetArticleAsync(string slug)
    {
        // Check cache first
        if (m_articles != null)
        {
            return m_articles.FirstOrDefault(a => a.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        // Direct load: find and load only the requested file
        return await GetArticleDirectAsync(slug, "articles");
    }
    
    /// <summary>
    /// Get a specific article by slug from a custom content folder.
    /// </summary>
    /// <param name="slug">Article slug.</param>
    /// <param name="contentFolder">Content folder name (e.g., "use-cases").</param>
    public async Task<ArticleCard?> GetArticleAsync(string slug, string contentFolder)
    {
        if (string.IsNullOrEmpty(contentFolder) || contentFolder == "articles")
        {
            return await GetArticleAsync(slug);
        }
        
        return await GetArticleDirectAsync(slug, contentFolder);
    }

    /// <summary>
    /// Load a specific article directly by slug without loading all articles.
    /// This is the fast path for single article pages.
    /// </summary>
    public async Task<ArticleCard?> GetArticleDirectAsync(string slug, string contentFolder)
    {
        try
        {
            var index = await GetContentIndexAsync();
            
            // Get files from the appropriate source
            List<string>? files = null;
            if (contentFolder == "articles")
            {
                files = index.Articles;
            }
            else if (index.Sections?.TryGetValue(contentFolder, out var sectionFiles) == true)
            {
                files = sectionFiles;
            }

            if (files == null)
                return null;
            
            // Find the file that matches the slug
            var matchingFile = files.FirstOrDefault(file => 
            {
                var (_, fileSlug) = SlugGenerator.GetOrderAndSlugFromFilename(file);
                return fileSlug.Equals(slug, StringComparison.OrdinalIgnoreCase);
            });

            if (matchingFile == null)
                return null;

            var markdown = await Http.GetStringAsync($"content/{contentFolder}/{matchingFile}");
            return ParseArticle(matchingFile, markdown, contentFolder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading article {slug} from {contentFolder}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get all articles from a custom content section.
    /// </summary>
    /// <param name="sectionFolder">Section folder name (e.g., "use-cases").</param>
    public async Task<List<ArticleCard>> GetSectionArticlesAsync(string sectionFolder)
    {
        var articles = new List<ArticleCard>();
        
        try
        {
            var index = await GetContentIndexAsync();
            if (index.Sections == null || !index.Sections.TryGetValue(sectionFolder, out var files))
            {
                return articles;
            }
            
            foreach (var file in files)
            {
                try
                {
                    var filePath = $"content/{sectionFolder}/{file}";
                    var markdown = await Http.GetStringAsync(filePath);
                    var article = ParseArticle(file, markdown, sectionFolder);
                    if (article != null)
                    {
                        articles.Add(article);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading {file} from {sectionFolder}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading section {sectionFolder}: {ex.Message}");
        }
        
        return articles.OrderBy(a => a.Order).ToList();
    }
    
    private ArticleCard? ParseArticle(string filename, string markdown, string contentFolder)
    {
        var (frontmatter, _) = MarkdownService.ParseWithFrontmatter<FrontmatterData>(markdown);
        if (frontmatter == null) return null;

        var (order, slug) = SlugGenerator.GetOrderAndSlugFromFilename(filename);
        var content = ExtractContentWithoutFrontmatter(markdown);

        // Extract embedded components [[Component ...]] and get processed content
        var (processedContent, components) = ContentParser.Transform(content);

        // Set base path for relative URL resolution
        var basePath = GetBasePathForArticle(filename, contentFolder);
        foreach (var component in components)
        {
            component.BasePath = basePath;
        }

        // Extract table of contents from headings (use frontmatter depth or default to 3)
        var tocDepth = frontmatter.TocDepth ?? 3;
        var toc = ExtractHeadings(content, tocDepth);

        // Render processed markdown (with component placeholders) to HTML
        var html = MarkdownService.ToHtml(processedContent);

        return new ArticleCard
        {
            Slug = slug,
            Order = order,
            Title = frontmatter.Title ?? slug,
            Description = !string.IsNullOrEmpty(frontmatter.Description)
                ? MarkdownService.ToHtmlInline(frontmatter.Description)
                : "",
            PublishDate = frontmatter.PublishDate,
            Tags = frontmatter.Tags ?? new List<string>(),
            MenuTitle = frontmatter.MenuTitle ?? "",
            ShowInMenu = frontmatter.ShowInMenu,
            RawContent = content,
            HtmlContent = html,
            TableOfContents = toc,
            EmbeddedComponents = components,
            TocDepth = tocDepth
        };
    }
    
    /// <summary>
    /// Extract headings from markdown content for TOC generation.
    /// Builds hierarchical structure with parent-child relationships.
    /// Skips headings inside code blocks.
    /// Handles duplicate headings by adding numeric suffixes like Markdig.
    /// </summary>
    /// <param name="markdown">Markdown content to extract headings from</param>
    /// <param name="maxDepth">Maximum heading depth to include (1=H1 only, 2=H1-H2, 3=H1-H3, etc.). Default is 3.</param>
    private static List<TocItem> ExtractHeadings(string markdown, int maxDepth = 3)
    {
        var flatHeadings = new List<TocItem>();
        var lines = markdown.Split('\n');
        bool inCodeBlock = false;
        var slugCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // First pass: extract all headings as flat list
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            // Track code block state (``` fenced code blocks)
            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            // Skip lines inside code blocks
            if (inCodeBlock)
                continue;

            if (!trimmed.StartsWith('#'))
                continue;

            // Must have space after # to be a valid markdown heading
            // This filters out #include, #pragma, etc.
            int level = 0;
            while (level < trimmed.Length && trimmed[level] == '#')
                level++;

            // Apply max depth filter
            if (level < 1 || level > maxDepth)
                continue;

            // Must have space after the #'s for valid heading
            if (level >= trimmed.Length || trimmed[level] != ' ')
                continue;

            // Extract heading text (remove #'s and space, then trim)
            var text = trimmed[(level + 1)..].Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            // Generate slug ID from text using shared utility
            var baseSlug = SlugGenerator.GenerateSlug(text);

            // Handle duplicates by adding suffix like Markdig does
            string id;
            if (slugCounts.TryGetValue(baseSlug, out int count))
            {
                slugCounts[baseSlug] = count + 1;
                id = $"{baseSlug}-{count}";
            }
            else
            {
                slugCounts[baseSlug] = 1;
                id = baseSlug;
            }

            flatHeadings.Add(new TocItem
            {
                Level = level,
                Id = id,
                Text = text
            });
        }

        // Second pass: build hierarchical structure
        return BuildTocHierarchy(flatHeadings);
    }

    /// <summary>
    /// Build hierarchical TOC structure from flat list of headings.
    /// Uses stack-based algorithm to create parent-child relationships.
    /// </summary>
    private static List<TocItem> BuildTocHierarchy(List<TocItem> flatHeadings)
    {
        var result = new List<TocItem>();
        var stack = new Stack<TocItem>();

        foreach (var heading in flatHeadings)
        {
            // Pop entries with equal or higher level (lower in hierarchy)
            while (stack.Count > 0 && stack.Peek().Level >= heading.Level)
                stack.Pop();

            if (stack.Count == 0)
            {
                // Top-level heading
                result.Add(heading);
            }
            else
            {
                // Add as child of current parent
                stack.Peek().Children.Add(heading);
            }

            stack.Push(heading);
        }

        return result;
    }

    #endregion

    #region Docs

    /// <summary>
    /// Get all documentation pages.
    /// </summary>
    public async Task<List<DocPage>> GetDocsAsync()
    {
        if (m_docs != null)
            return m_docs.Values.OrderBy(d => d.Order).ToList();

        var index = await GetContentIndexAsync();
        m_docs = new Dictionary<string, DocPage>();

        foreach (var file in index.Docs)
        {
            try
            {
                var markdown = await Http.GetStringAsync($"content/docs/{file}");
                var doc = ParseDocPage(file, markdown);
                if (doc != null)
                    m_docs[doc.Slug] = doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading doc {file}: {ex.Message}");
            }
        }

        // Build navigation links (Previous/Next)
        var sortedDocs = m_docs.Values.OrderBy(d => d.Order).ToList();
        for (int i = 0; i < sortedDocs.Count; i++)
        {
            if (i > 0)
            {
                sortedDocs[i].PreviousPage = new DocNavLink 
                { 
                    Slug = sortedDocs[i - 1].Slug, 
                    Title = sortedDocs[i - 1].Title 
                };
            }
            if (i < sortedDocs.Count - 1)
            {
                sortedDocs[i].NextPage = new DocNavLink 
                { 
                    Slug = sortedDocs[i + 1].Slug, 
                    Title = sortedDocs[i + 1].Title 
                };
            }
        }

        return sortedDocs;
    }

    /// <summary>
    /// Get a specific documentation page by slug.
    /// Uses cached data if available, otherwise loads directly from file.
    /// </summary>
    public async Task<DocPage?> GetDocAsync(string slug)
    {
        // Check cache first
        if (m_docs != null && m_docs.TryGetValue(slug, out var cachedDoc))
        {
            return cachedDoc;
        }

        // Direct load: find and load only the requested file
        return await GetDocDirectAsync(slug);
    }

    /// <summary>
    /// Load a specific doc page directly by slug without loading all docs.
    /// This is the fast path for single doc pages.
    /// Note: Previous/Next links won't be available in direct mode.
    /// </summary>
    public async Task<DocPage?> GetDocDirectAsync(string slug)
    {
        try
        {
            var index = await GetContentIndexAsync();
            
            // Find the file that matches the slug
            var matchingFile = index.Docs.FirstOrDefault(file => 
            {
                var (_, fileSlug) = SlugGenerator.GetOrderAndSlugFromFilename(file);
                return fileSlug.Equals(slug, StringComparison.OrdinalIgnoreCase);
            });

            if (matchingFile == null)
                return null;

            var markdown = await Http.GetStringAsync($"content/docs/{matchingFile}");
            return ParseDocPage(matchingFile, markdown);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading doc {slug}: {ex.Message}");
            return null;
        }
    }
    
    private DocPage? ParseDocPage(string filename, string markdown)
    {
        var (frontmatter, _) = MarkdownService.ParseWithFrontmatter<FrontmatterData>(markdown);
        if (frontmatter == null) return null;

        var (order, slug) = SlugGenerator.GetOrderAndSlugFromFilename(filename);
        var content = ExtractContentWithoutFrontmatter(markdown);

        // Extract embedded components [[Component ...]] and get processed content
        var (processedContent, components) = ContentParser.Transform(content);

        // Set base path for relative URL resolution
        var basePath = GetBasePathForDocs(filename);
        foreach (var component in components)
        {
            component.BasePath = basePath;
        }

        // Extract TOC from original content (before component placeholders)
        var toc = MarkdownService.ExtractTableOfContents(content);

        // Render processed markdown (with component placeholders) to HTML
        var html = MarkdownService.ToHtml(processedContent);

        return new DocPage
        {
            Slug = slug,
            Order = order,
            Title = frontmatter.Title ?? slug,
            Description = !string.IsNullOrEmpty(frontmatter.Description)
                ? MarkdownService.ToHtmlInline(frontmatter.Description)
                : "",
            ParentSlug = frontmatter.Parent ?? "",
            RawContent = content,
            HtmlContent = html,
            TableOfContents = toc,
            EmbeddedComponents = components
        };
    }

    #endregion

    #region Features

    /// <summary>
    /// Get all features sorted by order (from filename prefix).
    /// </summary>
    public async Task<List<FeatureCard>> GetFeaturesAsync()
    {
        if (m_features != null)
            return m_features;

        await m_featuresLock.WaitAsync();
        try
        {
            if (m_features != null)
                return m_features;

            var index = await GetContentIndexAsync();
            var features = new List<FeatureCard>();

            foreach (var file in index.Features)
            {
                try
                {
                    var markdown = await Http.GetStringAsync($"content/features/{file}");
                    var feature = ParseFeature(file, markdown);
                    if (feature != null)
                        features.Add(feature);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading feature {file}: {ex.Message}");
                }
            }

            m_features = features
                .GroupBy(f => f.Slug)
                .Select(g => g.First())
                .OrderBy(f => f.Order)
                .ToList();

            return m_features;
        }
        finally
        {
            m_featuresLock.Release();
        }
    }

    private FeatureCard? ParseFeature(string filename, string markdown)
    {
        var (frontmatter, _) = MarkdownService.ParseWithFrontmatter<FrontmatterData>(markdown);
        if (frontmatter == null) return null;

        var (order, slug) = SlugGenerator.GetOrderAndSlugFromFilename(filename);
        var content = ExtractContentWithoutFrontmatter(markdown);
        var html = MarkdownService.ToHtml(content);

        return new FeatureCard
        {
            Slug = slug,
            Order = order,
            Title = frontmatter.Title ?? slug,
            Description = !string.IsNullOrEmpty(frontmatter.Description)
                ? MarkdownService.ToHtmlInline(frontmatter.Description)
                : "",
            Icon = frontmatter.Icon ?? "",
            IconSvg = frontmatter.IconSvg ?? "",
            HtmlContent = html
        };
    }

    #endregion

    #region Tools
    
    /// <summary>
    /// Get the content index, loading it if necessary.
    /// Returns cached result on subsequent calls.
    /// </summary>
    public async Task<ContentIndex> GetContentIndexAsync()
    {
        if (m_contentIndex != null)
            return m_contentIndex;

        try
        {
            m_contentIndex = await Http.GetFromJsonAsync<ContentIndex>("content/index.json");
            
            if (m_contentIndex != null)
            {
                var totalItems = m_contentIndex.Blog.Count + m_contentIndex.Projects.Count + 
                                 m_contentIndex.Articles.Count + m_contentIndex.Docs.Count + 
                                 m_contentIndex.Features.Count;
                if (totalItems > 0)
                {
                    Console.WriteLine($"Loaded content index: {totalItems} items");
                }
            }
        }
        catch (HttpRequestException)
        {
            // File not found - content index not available (expected in Debug mode without generation)
            Console.WriteLine("Content index not found (Debug mode)");
            m_contentIndex = new ContentIndex();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading content index: {ex.Message}");
            m_contentIndex = new ContentIndex();
        }

        return m_contentIndex ?? new ContentIndex();
    }

    /// <summary>
    /// Get base path for docs relative URL resolution.
    /// </summary>
    private static string GetBasePathForDocs(string filename)
    {
        var lastSlash = filename.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var folder = filename[..lastSlash];
            return $"content/docs/{folder}";
        }
        return "content/docs";
    }

    /// <summary>
    /// Get base path for blog post relative URL resolution.
    /// </summary>
    private static string GetBasePathForBlogPost(string filename)
    {
        var lastSlash = filename.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var folder = filename[..lastSlash];
            return $"content/blog/{folder}";
        }
        return "content/blog";
    }

    /// <summary>
    /// Get base path for article relative URL resolution.
    /// </summary>
    private static string GetBasePathForArticle(string filename, string contentFolder)
    {
        var lastSlash = filename.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var folder = filename[..lastSlash];
            return $"content/{contentFolder}/{folder}";
        }
        return $"content/{contentFolder}";
    }

    private static string ExtractContentWithoutFrontmatter(string markdown)
    {
        // Remove YAML frontmatter
        var match = FrontmatterRegex().Match(markdown);
        if (match.Success)
            return markdown.Substring(match.Length).TrimStart();
        return markdown;
    }

    /// <summary>
    /// Get base path for relative URL resolution from filename.
    /// For "01-biography/index.md" returns "content/projects/01-biography"
    /// For "02-cheetah-solver.md" returns "content/projects"
    /// </summary>
    private static string GetBasePathFromFilename(string filename)
    {
        // If filename contains a folder (e.g., "01-biography/index.md")
        var lastSlash = filename.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var folder = filename[..lastSlash];
            return $"content/projects/{folder}";
        }
        return "content/projects";
    }

    [GeneratedRegex(@"^---[\s\S]*?---\s*", RegexOptions.Multiline)]
    private static partial Regex FrontmatterRegex();

    #endregion

    #region Properties

    private HttpClient Http { get; }
    
    private MarkdownService MarkdownService { get; }
    
    private ContentParser ContentParser { get; }

    #endregion
}