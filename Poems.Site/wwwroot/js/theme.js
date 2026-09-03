// Runs before styles are loaded to avoid flashing the wrong theme.
(() => {
    const storageKey = 'poems-theme';
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');
    const normalize = value => ['light', 'dark', 'system'].includes(value) ? value : 'system';
    let preference = 'system';

    try {
        preference = normalize(localStorage.getItem(storageKey));
    } catch {
        // Theme switching still works when browser storage is unavailable.
    }

    const applyTheme = () => {
        document.documentElement.dataset.theme = preference === 'system'
            ? (systemTheme.matches ? 'dark' : 'light')
            : preference;
        const select = document.getElementById('themeSelect');
        if (select) select.value = preference;
    };

    applyTheme();
    systemTheme.addEventListener('change', applyTheme);
    window.addEventListener('storage', event => {
        if (event.key === storageKey || event.key === null) {
            preference = normalize(event.newValue);
            applyTheme();
        }
    });

    document.addEventListener('DOMContentLoaded', () => {
        applyTheme();
        document.getElementById('themeSelect')?.addEventListener('change', event => {
            preference = normalize(event.target.value);
            try {
                localStorage.setItem(storageKey, preference);
            } catch {
                // Keep the preference for the current page even without storage.
            }
            applyTheme();
        });
    });
})();
