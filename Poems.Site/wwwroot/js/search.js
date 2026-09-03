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
        this.toastService = new ToastService();
        this.apiService = new ApiService();
        this.searchType = 'db';
        this.requestId = 0;
        this.poemRequestId = 0;
        this.activeIndex = -1;
        this.searchLoading = false;
        this.poemLoading = false;
        document.getElementById('elastic-tab-container').hidden = !options.elasticsearchEnabled;
        this.initializeEventListeners();
    }

    initializeEventListeners() {
        this.searchBox.addEventListener('input', () => {
            this.cancelSearch();
            this.clearSuggestions();
            this.queryTime.textContent = '—';
            this.status.textContent = '';
            if (this.searchBox.value.trim()) {
                this.searchTimeout = window.setTimeout(() => this.handleSearch(), 300);
            }
        });
        this.searchBox.addEventListener('focus', () => {
            if (this.resultsList.childElementCount || !this.suggestionsState.hidden) this.showSuggestions();
        });
        this.searchBox.addEventListener('keydown', event => this.handleKeyDown(event));
        document.addEventListener('pointerdown', event => {
            if (!event.target.closest('.search-control')) this.hideSuggestions();
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
                this.cancelSearch();
                this.clearSuggestions();
                this.searchBox.focus();
                this.handleSearch();
            });
        });

        window.addEventListener('resize', () => this.updateSuggestionsHeight(true), {passive: true});
        window.addEventListener('scroll', () => this.updateSuggestionsHeight(), {passive: true});
        window.visualViewport?.addEventListener('resize', () => this.updateSuggestionsHeight(true), {passive: true});
        window.visualViewport?.addEventListener('scroll', () => this.updateSuggestionsHeight(), {passive: true});
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

    async handleSearch() {
        this.cancelSearch();
        const query = this.searchBox.value.trim();
        if (!query) return;
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
        if (document.activeElement === this.searchBox) this.showSuggestions();
    }

    showMessage(message, isError = false) {
        this.clearSuggestions();
        this.resultCount.textContent = isError ? 'Ошибка поиска' : 'Результаты поиска';
        this.suggestionsState.textContent = message;
        this.suggestionsState.classList.toggle('is-error', isError);
        this.suggestionsState.hidden = false;
        this.status.textContent = message;
        if (document.activeElement === this.searchBox) this.showSuggestions();
    }

    handleKeyDown(event) {
        if (event.isComposing) return;
        if (event.key === 'Escape') {
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

    async selectPoem(poemId) {
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
            this.poemTitle.focus({preventScroll: true});
            this.poemContainer.scrollIntoView({block: 'start'});
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
if (page) new PoemSearch({elasticsearchEnabled: page.dataset.elasticsearchEnabled === 'true'});
