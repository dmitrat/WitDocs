// WitDocs Framework - JavaScript Interop
// Functions called from Blazor via JS.InvokeVoidAsync

// OutWit namespace for framework functions
window.outwit = window.outwit || {};

/**
 * Copy-to-clipboard for code blocks. A single delegated listener handles every
 * .code-copy button — including those Blazor renders later — so the markdown
 * author never writes any JS.
 */
document.addEventListener('click', function (e) {
    const button = e.target.closest('.code-copy');
    if (!button) return;

    const block = button.closest('.code-block');
    const code = block && block.querySelector('code');
    if (!code) return;

    const text = code.innerText;
    const done = function () {
        const original = button.dataset.label || button.textContent;
        button.dataset.label = original;
        button.textContent = 'Copied!';
        button.classList.add('is-copied');
        setTimeout(function () {
            button.textContent = button.dataset.label || 'Copy';
            button.classList.remove('is-copied');
        }, 1500);
    };

    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(done).catch(function () { /* ignore */ });
    } else {
        const ta = document.createElement('textarea');
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.select();
        try { document.execCommand('copy'); done(); } catch (_) { /* ignore */ }
        document.body.removeChild(ta);
    }
});

/**
 * Full-size view for images in markdown content.
 *
 * A screenshot is almost always wider than the column it is shown in, so the
 * picture on the page is a thumbnail whether or not it was meant to be one. One
 * delegated listener - the same shape as the copy button above - opens whichever
 * figure was clicked, including figures Blazor renders after this file has run.
 *
 * The overlay is built once, on first use: a site with no images never pays for it.
 */
(function () {
    var overlay = null;
    var picture = null;
    var caption = null;
    var opener = null;

    function build() {
        overlay = document.createElement('div');
        overlay.className = 'ow-lightbox';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.hidden = true;

        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'ow-lightbox__close';
        close.setAttribute('aria-label', 'Close');
        close.innerHTML = '&times;';

        picture = document.createElement('img');
        picture.className = 'ow-lightbox__image';

        caption = document.createElement('figcaption');
        caption.className = 'ow-lightbox__caption';

        var frame = document.createElement('figure');
        frame.className = 'ow-lightbox__frame';
        frame.appendChild(picture);
        frame.appendChild(caption);

        overlay.appendChild(close);
        overlay.appendChild(frame);
        document.body.appendChild(overlay);

        // Anywhere outside the picture closes, which is what the backdrop is for.
        overlay.addEventListener('click', function (e) {
            if (e.target !== caption) hide();
        });
    }

    function show(figure) {
        var img = figure.querySelector('img');
        if (!img) return;

        if (!overlay) build();

        picture.src = img.currentSrc || img.src;
        picture.alt = img.alt || '';

        var text = figure.querySelector('.ow-figure__caption');
        caption.textContent = text ? text.textContent : (img.alt || '');
        caption.hidden = !caption.textContent;

        overlay.setAttribute('aria-label', caption.textContent || 'Image');
        overlay.hidden = false;
        // A frame later, so the transition has a state to start from.
        requestAnimationFrame(function () { overlay.classList.add('is-open'); });

        document.documentElement.classList.add('ow-lightbox-open');
        overlay.querySelector('.ow-lightbox__close').focus();
    }

    function hide() {
        if (!overlay || overlay.hidden) return;

        overlay.classList.remove('is-open');
        overlay.hidden = true;
        document.documentElement.classList.remove('ow-lightbox-open');

        // Back to the picture that was clicked, so the keyboard does not lose its place.
        if (opener && document.contains(opener)) opener.focus();
        opener = null;
    }

    document.addEventListener('click', function (e) {
        var zoom = e.target.closest('.ow-figure__zoom');
        if (!zoom) return;

        var figure = zoom.closest('.ow-figure');
        if (!figure) return;

        opener = zoom;
        show(figure);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') hide();
    });

    // Leaving the page with the overlay up would otherwise keep the body locked.
    window.addEventListener('popstate', hide);
})();

/**
 * Get system theme preference (dark/light)
 * @returns {boolean} True if system prefers dark mode
 */
window.outwit.getSystemThemePreference = function () {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
};

/**
 * Set theme attribute on document element
 * @param {string} theme - Theme name ('dark' or 'light')
 */
window.outwit.setThemeAttribute = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
};

/**
 * Scroll to an element by its ID with smooth animation
 * @param {string} elementId - The ID of the element to scroll to
 */
window.scrollToElement = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
};

/**
 * Set or toggle theme
 * @param {string} theme - 'dark' or 'light'
 */
window.setTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
};

/**
 * Get current theme
 * @returns {string} Current theme
 */
window.getTheme = function () {
    return document.documentElement.getAttribute('data-theme') || 'dark';
};

/**
 * Render Cloudflare Turnstile widget explicitly for Blazor SPA
 * @param {object} dotNetRef - DotNet reference to the ContactForm component
 * @param {string} siteKey - Turnstile site key
 * @param {string} containerId - ID of the container element
 */
window.renderTurnstile = function (dotNetRef, siteKey, containerId) {
    // Wait for Turnstile to be available
    if (typeof turnstile === 'undefined') {
        // Retry after a short delay
        setTimeout(function () {
            window.renderTurnstile(dotNetRef, siteKey, containerId);
        }, 100);
        return;
    }

    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Turnstile container not found:', containerId);
        return;
    }

    // Clear any existing widget
    container.innerHTML = '';

    // Render Turnstile widget
    turnstile.render(container, {
        sitekey: siteKey,
        theme: 'auto',
        callback: function (token) {
            dotNetRef.invokeMethodAsync('OnTurnstileSuccess', token);
        },
        'error-callback': function () {
            dotNetRef.invokeMethodAsync('OnTurnstileError');
        },
        'expired-callback': function () {
            dotNetRef.invokeMethodAsync('OnTurnstileExpired');
        }
    });
};

/**
 * Reset Turnstile widget
 * @param {string} containerId - ID of the container element
 */
window.resetTurnstile = function (containerId) {
    if (typeof turnstile !== 'undefined') {
        const container = document.getElementById(containerId);
        if (container) {
            turnstile.reset(container);
        }
    }
};
