(function () {
    const typeSelect = document.getElementById('cs-type');
    const searchInput = document.getElementById('cs-search');
    const searchBtn = document.getElementById('cs-search-btn');
    const resultsContainer = document.getElementById('cs-results');
    const tmdbIdInput = document.getElementById('cs-tmdb-id');
    const titleInput = document.getElementById('cs-title');
    const imageUrlInput = document.getElementById('cs-image-url');
    const selectedBox = document.getElementById('cs-selected');
    const selectedTitle = document.getElementById('cs-selected-title');
    const selectedImage = document.getElementById('cs-selected-image');
    const episodeFields = document.getElementById('cs-episode-fields');
    const seasonSelect = document.getElementById('cs-season');
    const episodeSelect = document.getElementById('cs-episode');
    const seasonHidden = document.getElementById('cs-season-hidden');
    const submitBtn = document.getElementById('cs-submit');

    if (!typeSelect) return;

    let currentType = '1';
    let currentSeriesTitle = '';

    function resetSelection() {
        tmdbIdInput.value = '';
        titleInput.value = '';
        imageUrlInput.value = '';
        currentSeriesTitle = '';
        selectedBox.style.display = 'none';
        submitBtn.disabled = true;
    }

    function renderResults(items) {
        resultsContainer.innerHTML = '';
        if (!items || !items.length) {
            resultsContainer.innerHTML = '<p class="text-muted">Nenhum conteúdo encontrado.</p>';
            return;
        }
        items.forEach(item => {
            const div = document.createElement('div');
            div.className = 'd-flex align-items-center gap-2 p-2 border-bottom';
            div.style.cursor = 'pointer';
            const img = item.imageUrl
                ? `<img src="${item.imageUrl}" style="height:40px;" />`
                : '';
            div.innerHTML = `${img}<div><strong>${item.title}</strong><br><small class="text-muted">${item.releaseYear ?? ''}</small></div>`;
            div.addEventListener('click', () => selectResult(item));
            resultsContainer.appendChild(div);
        });
    }

    function selectResult(item) {
        tmdbIdInput.value = item.tmdbId;
        titleInput.value = item.title;
        imageUrlInput.value = item.imageUrl || '';
        currentSeriesTitle = item.title;
        selectedTitle.textContent = item.title;
        selectedImage.src = item.imageUrl || '';
        selectedImage.style.display = item.imageUrl ? '' : 'none';
        selectedBox.style.display = '';
        resultsContainer.innerHTML = '';

        if (currentType === '3') {
            episodeFields.style.display = '';
            loadSeasons(item.tmdbId);
        } else {
            episodeFields.style.display = 'none';
            seasonHidden.value = '';
            episodeSelect.innerHTML = '';
        }
        submitBtn.disabled = false;
    }

    async function loadSeasons(tmdbId) {
        seasonSelect.innerHTML = '<option value="">Carregando...</option>';
        try {
            const response = await fetch(`/ContentCalendar/GetSeriesDetails?tmdbId=${tmdbId}`);
            const result = await response.json();
            seasonSelect.innerHTML = '<option value="">Selecione</option>';
            const total = result.isSuccess ? (result.numberOfSeasons || 1) : 10;
            for (let i = 1; i <= total; i++) {
                const opt = document.createElement('option');
                opt.value = i;
                opt.textContent = `${i}ª Temporada`;
                seasonSelect.appendChild(opt);
            }
        } catch (e) {
            seasonSelect.innerHTML = '<option value="">Selecione</option>';
            for (let i = 1; i <= 10; i++) {
                const opt = document.createElement('option');
                opt.value = i;
                opt.textContent = `${i}ª Temporada`;
                seasonSelect.appendChild(opt);
            }
        }
    }

    async function loadEpisodes() {
        const tmdbId = tmdbIdInput.value;
        const season = seasonSelect.value;
        if (!tmdbId || !season) return;
        seasonHidden.value = season;
        episodeSelect.innerHTML = '<option value="">Carregando...</option>';
        const response = await fetch(`/ContentCalendar/GetEpisodes?tmdbId=${tmdbId}&season=${season}`);
        const result = await response.json();
        episodeSelect.innerHTML = '';
        if (!result.isSuccess || !result.data || !result.data.length) {
            episodeSelect.innerHTML = '<option value="">Nenhum episódio</option>';
            submitBtn.disabled = true;
            return;
        }
        result.data.forEach(ep => {
            const opt = document.createElement('option');
            opt.value = ep.episodeNumber;
            opt.textContent = `E${ep.episodeNumber} - ${ep.name}`;
            opt.dataset.title = ep.name;
            opt.dataset.banner = ep.bannerUrl;
            opt.dataset.season = ep.seasonNumber;
            episodeSelect.appendChild(opt);
        });
        submitBtn.disabled = false;
    }

    async function doSearch() {
        const query = searchInput.value.trim();
        if (!query) return;
        resultsContainer.innerHTML = '<p class="text-muted">Buscando...</p>';
        const endpoint = currentType === '2'
            ? `/ContentCalendar/SearchSeries?query=${encodeURIComponent(query)}`
            : `/ContentCalendar/SearchMovies?query=${encodeURIComponent(query)}`;
        // For episodes, search series
        const url = currentType === '3'
            ? `/ContentCalendar/SearchSeries?query=${encodeURIComponent(query)}`
            : endpoint;
        try {
            const response = await fetch(url);
            const result = await response.json();
            if (!result.isSuccess) {
                resultsContainer.innerHTML = `<p class="text-danger">${result.error || 'Falha na busca.'}</p>`;
                return;
            }
            renderResults(result.data);
        } catch (e) {
            resultsContainer.innerHTML = '<p class="text-danger">Falha de comunicação com o TMDB.</p>';
        }
    }

    typeSelect.addEventListener('change', () => {
        currentType = typeSelect.value;
        resetSelection();
        resultsContainer.innerHTML = '';
        episodeFields.style.display = 'none';
    });

    searchBtn.addEventListener('click', doSearch);
    searchInput.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); doSearch(); } });

    seasonSelect.addEventListener('change', loadEpisodes);

    episodeSelect.addEventListener('change', () => {
        const opt = episodeSelect.selectedOptions[0];
        if (!opt) return;
        const epName = opt.dataset.title || '';
        const season = opt.dataset.season || seasonSelect.value;
        const epNumber = opt.value;
        titleInput.value = currentSeriesTitle
            ? `${currentSeriesTitle} • ${(epName || titleInput.value)}`: (epName || titleInput.value);
        imageUrlInput.value = opt.dataset.banner || imageUrlInput.value;
        selectedTitle.textContent = titleInput.value;
        selectedImage.src = imageUrlInput.value;
        seasonHidden.value = season;
    });

    // Before submit, set hidden season number for episodes
    document.getElementById('createScheduleForm').addEventListener('submit', () => {
        if (currentType === '3') {
            seasonHidden.name = 'SeasonNumber';
            // ensure SeasonNumber hidden input has name
        }
    });
})();
