// The one implementation of "apply the Theme and Mode to the page" (design section 11).
// A classic script, loaded synchronously in <head> of index.html so the stored choice is applied before first paint; the Blazor
// interop (Web/Theming/ThemeInterop.cs) then calls window.FitnessTheme.apply on every change. Settings are the source of truth:
// the localStorage mirror below is only a copy for the next load's first paint, rewritten by apply().
// A blocked or cleared mirror, or an unknown stored value, simply means the defaults (Keep, System).
(function () {
    'use strict';

    const MIRROR_KEY = 'fitness-app.theme-mirror';
    const THEME_ATTRIBUTE = 'data-theme';
    const MODE_ATTRIBUTE = 'data-mode';
    const DEFAULT_THEME = 'keep';
    const DEFAULT_MODE = 'system';
    const THEMES = ['keep', 'clean'];
    const MODES = ['system', 'dark', 'light'];

    // Browser chrome colours: splash.json for Keep dark, Clean dark and Clean light; Keep light is the Keep light --surface token.
    const CHROME_COLOURS = {
        'keep-dark': '#4A4237',
        'keep-light': '#E8DFC8',
        'clean-dark': '#111315',
        'clean-light': '#F4F5F7',
    };

    const schemeQuery = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;
    let current = { theme: DEFAULT_THEME, mode: DEFAULT_MODE };

    function normalise(theme, mode) {
        return {
            theme: THEMES.includes(theme) ? theme : DEFAULT_THEME,
            mode: MODES.includes(mode) ? mode : DEFAULT_MODE,
        };
    }

    function resolveMode(mode) {
        if (mode === 'system') return schemeQuery && !schemeQuery.matches ? 'light' : 'dark';
        return mode;
    }

    function render() {
        const resolved = resolveMode(current.mode);
        const root = document.documentElement;
        root.setAttribute(THEME_ATTRIBUTE, current.theme);
        root.setAttribute(MODE_ATTRIBUTE, resolved);

        let meta = document.querySelector('meta[name="theme-color"]');
        if (!meta && document.head) {
            meta = document.createElement('meta');
            meta.name = 'theme-color';
            document.head.appendChild(meta);
        }
        if (meta) meta.setAttribute('content', CHROME_COLOURS[current.theme + '-' + resolved]);
    }

    function readMirror() {
        try {
            const stored = JSON.parse(window.localStorage.getItem(MIRROR_KEY));
            return normalise(stored && stored.theme, stored && stored.mode);
        } catch {
            return normalise();
        }
    }

    function writeMirror() {
        try {
            window.localStorage.setItem(MIRROR_KEY, JSON.stringify(current));
        } catch {
            // Storage blocked: the next load starts with the default look for a moment, then the app reconciles.
        }
    }

    /** Applies a choice ("keep"|"clean", "system"|"dark"|"light"; anything else is the default) and remembers it in the mirror. */
    function apply(theme, mode) {
        current = normalise(theme, mode);
        render();
        writeMirror();
    }

    /** Applies the mirrored choice without rewriting it. Runs once, at load. */
    function applyMirrored() {
        current = readMirror();
        render();
    }

    if (schemeQuery) {
        // The phone's scheme changed: only System follows it.
        schemeQuery.addEventListener('change', function () {
            if (current.mode === 'system') render();
        });
    }

    window.FitnessTheme = { apply: apply, applyMirrored: applyMirrored };
    applyMirrored();
}());
