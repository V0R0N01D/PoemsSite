﻿import {ToastService} from "./toast.js";
import {ApiService} from "./apiService.js";

export class PoemSearch {
    constructor(options = {}) {
        this.searchBox = document.getElementById("searchBox");
        this.suggestionsBox = document.getElementById("suggestions");
        this.queryTime = document.getElementById('queryTime');
        this.elasticTabContainer = document.getElementById('elastic-tab-container');

        this.poemContainer = document.getElementById('poemContainer');
        this.poemTitle = document.getElementById('poemTitle');
        this.poemAuthor = document.getElementById('poemAuthor');
        this.poemContent = document.getElementById('poemContent');

        this.searchType = 'db';

        this.toastService = new ToastService();
        this.apiService = new ApiService();

        this.elasticAvailable = options.elasticsearchEnabled ?? false;
        this.isLoading = false;

        this.initializeApp();
    }

    async initializeApp() {
        if (this.elasticAvailable) {
            this.elasticTabContainer.style.display = 'block';
        }

        this.loadingIndicator = document.createElement('div');
        this.loadingIndicator.className = 'spinner-grow spinner-grow-sm text-primary loading-indicator';
        this.loadingIndicator.setAttribute('role', 'status');
        this.loadingIndicator.style.opacity = '0.7';
        this.loadingIndicator.style.width = '1.2rem';
        this.loadingIndicator.style.height = '1.2rem';
        this.loadingIndicator.style.display = 'none';
        this.loadingIndicator.innerHTML = '<span class="visually-hidden">Загрузка...</span>';

        const searchContainer = document.querySelector('.search-container .position-relative');
        searchContainer.appendChild(this.loadingIndicator);

        this.initializeEventListeners();
    }

    initializeEventListeners() {
        this.searchBox.addEventListener("input", () => {
            clearTimeout(this.searchTimeout);
            this.searchTimeout = setTimeout(() => this.handleSearch(), 500);
        });

        this.searchBox.addEventListener("focus", () => {
            if (this.searchBox.value.trim()) {
                this.showSuggestions();
            }
        });
        document.addEventListener("click",
            (e) => this.handleClickOutside(e));
        this.suggestionsBox.addEventListener("click",
            (e) => this.handleSelectPoem(e));

        document.querySelectorAll('#searchTabs button')
            .forEach(button => {
                button.addEventListener('click', (e) => {
                    const searchType = e.target.getAttribute('data-search-type');
                    this.updateSearchType(searchType);
                    if (this.searchBox.value.trim()) {
                        this.handleSearch();
                    }
                });
            });
    }

    updateSearchType(searchType) {
        this.searchType = searchType;
    }

    setLoading(isLoading) {
        this.isLoading = isLoading;
        if (isLoading) {
            this.loadingIndicator.style.display = 'inline-block';
        } else {
            this.loadingIndicator.style.display = 'none';
        }
    }

    handleClickOutside(e) {
        if (!this.searchBox.contains(e.target)
            && !this.suggestionsBox.contains(e.target)) {
            this.hideSuggestions();
        }
    }

    async handleSearch() {
        const query = this.searchBox.value.trim();
        if (!query) {
            this.hideSuggestions();
            return;
        }

        this.setLoading(true);
        const result = await this.apiService.searchPoems(query, this.searchType);
        this.setLoading(false);

        if (!result.success) {
            this.showSearchError(result.error);
            return;
        }

        this.showSearchStatus(result.result);
        this.updateSuggestions(result.result.poems);
    }

    async handleSelectPoem(event) {
        const button = event.target.closest("button.dropdown-item");
        if (!button) return;
        
        const poemId = button.dataset.poemId;

        this.setLoading(true);
        const result = await this.apiService.getPoemById(poemId);
        this.setLoading(false);

        if (!result.success) {
            this.toastService.showError(result.error)
            return;
        }

        this.setPoemDataInBox(result.result);
        this.hideSuggestions();
    }

    showSearchStatus(result) {
        this.queryTime.textContent = result.queryTimeMs;
    }

    updateSuggestions(poems) {
        this.suggestionsBox.innerHTML = "";

        if (poems.length > 0) {
            this.showSuggestions();
            poems.forEach(poem => this.addSuggestion(poem));
        } else {
            this.showSearchNoResults();
        }
    }

    addSuggestion(poem) {
        const button = document.createElement("button");
        button.className = "dropdown-item";
        button.innerHTML = `
            <div class="suggestion-title">${poem.title}</div>
            <div class="suggestion-author text-muted">${poem.author_name}</div>
            <div class="suggestion-rank text-muted">Релевантность: ${poem.rank.toFixed(3)}</div>
        `;
        button.dataset.poemId = poem.id;
        this.suggestionsBox.appendChild(button);
    }

    setPoemDataInBox(poem) {
        this.poemTitle.textContent = poem.title;
        this.poemAuthor.textContent = `Автор: ${poem.authorName}`;
        this.poemContent.textContent = poem.content;

        this.poemContainer.style.display = 'block';
    }

    showSearchNoResults() {
        this.showSuggestions();
        this.suggestionsBox.innerHTML = `
            <div class="dropdown-item text-muted text-center py-2">
                Ничего не найдено.
            </div>
        `;
    }

    showSearchError(message) {
        this.showSuggestions();
        this.suggestionsBox.innerHTML = `
            <div class="dropdown-item text-danger text-center py-2">
                ${message}
            </div>
        `;
    }

    showSuggestions() {
        this.suggestionsBox.classList.add("show");
    }

    hideSuggestions() {
        this.suggestionsBox.classList.remove("show");
    }
}