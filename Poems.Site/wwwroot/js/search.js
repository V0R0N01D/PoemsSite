import {ToastService} from './toast.js';
import {ApiService} from './apiService.js';

export class PoemSearch {
    constructor(options = {}) {
        this.searchBox = document.getElementById('searchBox');
        this.suggestionsBox = document.getElementById('suggestions');
        this.resultsList = document.getElementById('searchResults');
        this.resultCount = document.getElementById('resultCount');
        this.suggestionsState = document.getElementById('suggestionsState');
        this.status = document.getElementById('searchStatus');
        this.queryTime = document.getElementById('queryTime');
        this.loadingIndicator = document.getElementById('loadingIndicator');
        this.poemContainer = document.getElementById('poemContainer');
        this.poemTitle = document.getElementById('poemTitle');
        this.poemAuthor = document.getElementById('poemAuthor');
        this.poemContent = document.getElementById('poemContent');
        this.scrollTopButton = document.getElementById('scrollTopButton');
        this.toastService = new ToastService();
        this.apiService = new ApiService();
        this.searchType = 'db';
        this.requestId = 0;
        this.poemRequestId = 0;
        this.activeIndex = -1;
        this.searchLoading = false;
        this.poemLoading = false;
        this.elasticAvailable = options.elasticsearchEnabled ?? false;
        this.vectorAvailable = options.vectorSearchEnabled ?? false;
        this.reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
        document.getElementById('elastic-tab-container').hidden = !this.elasticAvailable;
        document.getElementById('vector-tab-container').hidden = !this.vectorAvailable;
        this.initializeEventListeners();
        window.history.scrollRestoration = 'manual';
        this.restoreNavigation({initial: true});
    }

    initializeEventListeners() {
        this.searchBox.addEventListener('input', () => {
            this.showResultsAfterScroll = true;
            this.cancelSearch();
            this.rememberSearch();
            this.clearSuggestions();
            this.queryTime.textContent = '—';
            this.status.textContent = '';
            if (this.searchBox.value.trim()) {
                this.searchTimeout = window.setTimeout(() => this.handleSearch(), this.searchType === 'vector' ? 700 : 300);
            }
        });
        this.searchBox.addEventListener('focus', () => {
            if (!this.isScrolling && (this.resultsList.childElementCount || !this.suggestionsState.hidden)) {
                this.showSuggestions();
            }
        });
        this.searchBox.addEventListener('keydown', event => this.handleKeyDown(event));
        document.addEventListener('pointerdown', event => {
            if (!event.target.closest('.search-control')) {
                this.showResultsAfterScroll = false;
                this.hideSuggestions();
            }
        });
        document.addEventListener('focusin', event => {
            if (!event.target.closest('.search-control')) this.hideSuggestions();
        });
        // Keep the combobox focused when selecting an option with a mouse.
        this.resultsList.addEventListener('mousedown', event => event.preventDefault());
        this.resultsList.addEventListener('click', event => {
            const option = event.target.closest('[data-poem-id]');
            if (option) this.selectPoem(option.dataset.poemId);
        });
        document.querySelectorAll('#searchTabs button').forEach(button => {
            button.addEventListener('click', () => {
                this.searchType = button.dataset.searchType;
                this.updateSearchMode();
                this.rememberSearch();
                document.querySelectorAll('#searchTabs button').forEach(tab => {
                    tab.setAttribute('aria-pressed', String(tab === button));
                });
                this.cancelSearch();
                this.clearSuggestions();
                this.searchBox.focus();
                this.handleSearch();
            });
        });
        document.querySelectorAll('[data-query]').forEach(button => {
            button.addEventListener('click', () => {
                this.searchBox.value = button.dataset.query;
                this.rememberSearch();
                this.cancelSearch();
                this.clearSuggestions();
                this.searchBox.focus();
                this.handleSearch();
            });
        });

        this.scrollTopButton.addEventListener('click', () => this.returnToSearch());
        document.querySelector('.back-to-search').addEventListener('click', event => {
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            this.returnToSearch();
        });
        // Browser buttons, keyboard shortcuts and mouse back/forward buttons all emit popstate.
        window.addEventListener('popstate', () => this.restoreNavigation());
        window.addEventListener('resize', () => {
            this.updateSuggestionsHeight(!this.isScrolling);
            this.updateScrollTopButton();
        }, {passive: true});
        window.addEventListener('scroll', () => {
            this.updateSuggestionsHeight();
            this.updateScrollTopButton();
        }, {passive: true});
        window.visualViewport?.addEventListener('resize', () => this.updateSuggestionsHeight(true), {passive: true});
        window.visualViewport?.addEventListener('scroll', () => this.updateSuggestionsHeight(), {passive: true});
    }

    rememberSearch() {
        this.cancelPoemRequest();
        this.writeNavigation(null, {replace: this.historyPoemId === null});
        document.title = 'Поиск русской поэзии — PoemsSite';
    }

    writeNavigation(poemId, {replace = false} = {}) {
        const url = new URL(window.location.href);
        const query = this.searchBox.value.trim();
        if (query) url.searchParams.set('query', query);
        else url.searchParams.delete('query');
        if (this.searchType !== 'db') url.searchParams.set('engine', this.searchType);
        else url.searchParams.delete('engine');
        if (poemId) url.searchParams.set('poem', poemId);
        else url.searchParams.delete('poem');
        url.hash = '';
        const method = replace || url.href === window.location.href ? 'replaceState' : 'pushState';
        window.history[method]({...window.history.state, poemsSite: true}, '', url);
        this.historyPoemId = poemId;
    }

    restoreNavigation({initial = false} = {}) {
        this.cancelSearch();
        this.clearSuggestions();
        const parameters = new URL(window.location.href).searchParams;
        this.searchBox.value = parameters.get('query') ?? '';
        const engine = parameters.get('engine');
        this.searchType = this.vectorAvailable && engine === 'vector' ? 'vector'
            : this.elasticAvailable && engine === 'elastic' ? 'elastic' : 'db';
        this.updateSearchMode();
        document.querySelectorAll('#searchTabs button').forEach(button => {
            button.setAttribute('aria-pressed', String(button.dataset.searchType === this.searchType));
        });
        this.queryTime.textContent = '—';
        const poemId = parameters.get('poem');
        this.historyPoemId = /^[1-9]\d*$/.test(poemId ?? '') && Number(poemId) <= 2147483647 ? poemId : null;
        if (this.historyPoemId) {
            this.selectPoem(this.historyPoemId, {pushHistory: false});
        } else if (!initial || this.searchBox.value.trim()) {
            this.returnToSearch({pushHistory: false});
        } else {
            this.writeNavigation(null, {replace: true});
        }
    }

    returnToSearch({pushHistory = true} = {}) {
        this.cancelPoemRequest();
        this.cancelSearch();
        if (pushHistory) this.writeNavigation(null);
        document.title = 'Поиск русской поэзии — PoemsSite';
        // Keep the poem in the document so its removal cannot jump the viewport to the top.
        this.scrollToSection(null, this.searchBox);
        if (this.searchBox.value.trim()) this.handleSearch();
    }

    scrollToSection(section, focusTarget) {
        this.scrollCleanup?.();
        this.isScrolling = true;
        this.showResultsAfterScroll = focusTarget === this.searchBox;
        this.hideSuggestions();
        focusTarget.focus({preventScroll: true});
        let timer;
        const cleanup = () => {
            window.clearTimeout(timer);
            window.removeEventListener('scroll', settle);
            window.removeEventListener('scrollend', finish);
            this.scrollCleanup = null;
        };
        const finish = () => {
            cleanup();
            this.isScrolling = false;
            this.updateScrollTopButton();
            if (this.showResultsAfterScroll && document.activeElement === this.searchBox &&
                (this.resultsList.childElementCount || !this.suggestionsState.hidden)) {
                this.showSuggestions();
            }
        };
        // The idle fallback also covers browsers without scrollend and an already-reached target.
        const settle = () => {
            window.clearTimeout(timer);
            timer = window.setTimeout(finish, 150);
        };
        this.scrollCleanup = cleanup;
        window.addEventListener('scroll', settle, {passive: true});
        window.addEventListener('scrollend', finish);
        settle();
        const behavior = this.reducedMotion.matches ? 'instant' : 'smooth';
        if (section) section.scrollIntoView({block: 'start', behavior});
        else {
            const field = this.searchBox.getBoundingClientRect();
            const viewportHeight = window.visualViewport?.height ?? window.innerHeight;
            const top = viewportHeight - (field.bottom + window.scrollY) < 152
                ? Math.max(0, field.top + window.scrollY - 24)
                : 0;
            window.scrollTo({top, behavior});
        }
    }

    updateScrollTopButton() {
        this.scrollTopButton.hidden = this.poemContainer.hidden || this.searchBox.getBoundingClientRect().bottom > 0;
    }

    cancelPoemRequest() {
        this.poemController?.abort();
        this.poemRequestId++;
        this.poemLoading = false;
        this.updateLoading();
    }

    cancelSearch() {
        window.clearTimeout(this.searchTimeout);
        this.searchController?.abort();
        this.requestId++;
        this.searchLoading = false;
        this.updateLoading();
    }

    updateLoading() {
        this.loadingIndicator.hidden = !this.searchLoading && !this.poemLoading;
        this.searchBox.setAttribute('aria-busy', String(this.searchLoading));
        this.poemContainer.setAttribute('aria-busy', String(this.poemLoading));
    }

    updateSearchMode() {
        const vector = this.searchType === 'vector';
        this.searchBox.placeholder = vector ? 'Тема, настроение или образ…' : 'Автор, название или строка…';
        if (vector) this.searchBox.maxLength = 1000;
        else this.searchBox.removeAttribute('maxlength');
        document.getElementById('textSearchHint').hidden = vector;
        document.getElementById('vectorSearchHint').hidden = !vector;
    }

    async handleSearch() {
        this.cancelSearch();
        const query = this.searchBox.value.trim();
        if (!query) return;
        if (this.searchType === 'vector' && (query.length < 3 || query.length > 1000)) {
            this.queryTime.textContent = '—';
            this.showMessage('Введите от 3 до 1000 символов для поиска по смыслу.');
            return;
        }
        const requestId = this.requestId;
        this.searchController = new AbortController();
        this.searchLoading = true;
        this.updateLoading();
        this.status.textContent = 'Ищем стихотворения…';
        try {
            const result = await this.apiService.searchPoems(query, this.searchType, this.searchController.signal);
            if (requestId !== this.requestId) return;
            if (!result.success) {
                this.showMessage(result.error, true);
                return;
            }
            const time = Number(result.result.queryTimeMs);
            this.queryTime.textContent = `${time.toLocaleString('ru-RU', {maximumFractionDigits: 1})} мс`;
            this.updateSuggestions(result.result.poems);
        } catch (error) {
            if (error.name !== 'AbortError' && requestId === this.requestId) {
                this.showMessage('Не удалось выполнить поиск. Попробуйте ещё раз.', true);
            }
        } finally {
            if (requestId === this.requestId) {
                this.searchLoading = false;
                this.updateLoading();
            }
        }
    }

    clearSuggestions() {
        this.hideSuggestions();
        this.resultsList.replaceChildren();
        this.suggestionsState.hidden = true;
    }

    updateSuggestions(poems) {
        this.clearSuggestions();
        if (!poems.length) {
            this.showMessage('Ничего не найдено. Попробуйте другое слово или имя автора.');
            return;
        }
        const fragment = document.createDocumentFragment();
        poems.forEach((poem, index) => {
            const option = document.createElement('li');
            option.id = `poem-option-${index}`;
            option.className = 'suggestion-item';
            option.setAttribute('role', 'option');
            option.setAttribute('aria-selected', 'false');
            option.dataset.poemId = poem.id;
            const title = document.createElement('span');
            title.className = 'suggestion-title';
            title.textContent = poem.title || 'Без названия';
            const author = document.createElement('span');
            author.className = 'suggestion-author';
            author.textContent = poem.author_name;
            const relevance = document.createElement('span');
            relevance.className = 'suggestion-relevance';
            const rank = Number(poem.rank);
            relevance.textContent = `Релевантность: ${Number.isFinite(rank)
                ? rank.toLocaleString('ru-RU', {minimumFractionDigits: 3, maximumFractionDigits: 3})
                : '—'}`;
            relevance.title = 'Чем выше значение, тем вероятнее, что результат соответствует запросу';
            const details = document.createElement('span');
            details.className = 'suggestion-details';
            details.append(author, relevance);
            option.append(title, details);
            fragment.appendChild(option);
        });
        this.resultsList.appendChild(fragment);
        this.resultsList.scrollTop = 0;
        this.resultCount.textContent = `Показано произведений: ${poems.length}`;
        this.status.textContent = `${this.resultCount.textContent}. Используйте стрелки для выбора и Enter для открытия.`;
        if (document.activeElement === this.searchBox && !this.isScrolling) this.showSuggestions();
    }

    showMessage(message, isError = false) {
        this.clearSuggestions();
        this.resultCount.textContent = isError ? 'Ошибка поиска' : 'Результаты поиска';
        this.suggestionsState.textContent = message;
        this.suggestionsState.classList.toggle('is-error', isError);
        this.suggestionsState.hidden = false;
        this.status.textContent = message;
        if (document.activeElement === this.searchBox && !this.isScrolling) this.showSuggestions();
    }

    handleKeyDown(event) {
        if (event.isComposing) return;
        if (event.key === 'Escape') {
            event.preventDefault();
            this.showResultsAfterScroll = false;
            this.cancelSearch();
            this.hideSuggestions();
            return;
        }
        const options = this.resultsList.children;
        if ((event.key === 'ArrowDown' || event.key === 'ArrowUp') && options.length) {
            event.preventDefault();
            this.showSuggestions();
            const nextIndex = this.activeIndex < 0
                ? (event.key === 'ArrowDown' ? 0 : options.length - 1)
                : this.activeIndex + (event.key === 'ArrowDown' ? 1 : -1);
            this.setActiveOption((nextIndex + options.length) % options.length);
        } else if (event.key === 'Enter') {
            event.preventDefault();
            if (!this.suggestionsBox.hidden && this.activeIndex >= 0) {
                this.selectPoem(options[this.activeIndex].dataset.poemId);
            } else {
                this.handleSearch();
            }
        } else if (event.key === 'Tab') {
            this.showResultsAfterScroll = false;
            this.hideSuggestions();
        }
    }

    setActiveOption(index) {
        this.activeIndex = index;
        Array.from(this.resultsList.children).forEach((option, optionIndex) => {
            option.setAttribute('aria-selected', String(optionIndex === index));
        });
        const option = this.resultsList.children[index];
        this.searchBox.setAttribute('aria-activedescendant', option.id);
        option.scrollIntoView({block: 'nearest'});
    }

    showSuggestions() {
        this.suggestionsBox.hidden = false;
        this.searchBox.setAttribute('aria-expanded', 'true');
        this.updateSuggestionsHeight(true);
    }

    hideSuggestions() {
        this.suggestionsBox.hidden = true;
        this.searchBox.setAttribute('aria-expanded', 'false');
        this.searchBox.removeAttribute('aria-activedescendant');
        this.activeIndex = -1;
        Array.from(this.resultsList.children).forEach(option => option.setAttribute('aria-selected', 'false'));
    }

    updateSuggestionsHeight(ensureVisible = false) {
        if (this.suggestionsBox.hidden) return;
        const viewport = window.visualViewport;
        const viewportBottom = viewport ? viewport.offsetTop + viewport.height : window.innerHeight;
        let available = viewportBottom - this.suggestionsBox.getBoundingClientRect().top - 24;
        if (ensureVisible && available < 120 && document.activeElement === this.searchBox) {
            this.searchBox.scrollIntoView({block: 'start'});
            available = viewportBottom - this.suggestionsBox.getBoundingClientRect().top - 24;
        }
        this.suggestionsBox.style.setProperty('--suggestions-height', `${Math.max(0, available)}px`);
    }

    async selectPoem(poemId, {pushHistory = true} = {}) {
        this.cancelSearch();
        this.hideSuggestions();
        this.poemController?.abort();
        this.poemController = new AbortController();
        const requestId = ++this.poemRequestId;
        this.poemLoading = true;
        this.updateLoading();
        this.status.textContent = 'Открываем стихотворение…';
        try {
            const result = await this.apiService.getPoemById(poemId, this.poemController.signal);
            if (requestId !== this.poemRequestId) return;
            if (!result.success) {
                this.toastService.showError(result.error);
                this.status.textContent = 'Не удалось открыть стихотворение.';
                return;
            }
            this.poemTitle.textContent = result.result.title || 'Без названия';
            this.poemAuthor.textContent = result.result.authorName;
            this.poemContent.textContent = result.result.content;
            this.poemContainer.hidden = false;
            document.getElementById('readingPlaceholder').hidden = true;
            this.status.textContent = 'Стихотворение открыто.';
            if (pushHistory) this.writeNavigation(poemId);
            document.title = `${this.poemTitle.textContent} — PoemsSite`;
            this.scrollToSection(this.poemContainer, this.poemTitle);
        } catch (error) {
            if (error.name !== 'AbortError' && requestId === this.poemRequestId) {
                this.toastService.showError('Не удалось открыть стихотворение. Попробуйте ещё раз.');
            }
        } finally {
            if (requestId === this.poemRequestId) {
                this.poemLoading = false;
                this.updateLoading();
            }
        }
    }
}

const page = document.querySelector('.search-page');
if (page) new PoemSearch({
    elasticsearchEnabled: page.dataset.elasticsearchEnabled === 'true',
    vectorSearchEnabled: page.dataset.vectorSearchEnabled === 'true'
});
