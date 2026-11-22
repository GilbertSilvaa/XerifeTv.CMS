(function () {
    const root = document.getElementById('castCrewStep');
    if (!root) return;

    const form = root.closest('form');
    const controller = root.dataset.controller || '';
    const listEl = document.getElementById('credits-list');
    const emptyEl = document.getElementById('credits-empty');
    const serverInputs = document.getElementById('server-credit-inputs');
    const initialJson = document.getElementById('initial-credits');
    const importButton = document.getElementById('btn-import-credits');
    const importHint = document.getElementById('import-credits-hint');
    const importFeedback = document.getElementById('import-credits-feedback');
    const imdbInput = document.getElementById('imdbId');

    const creditModalEl = document.getElementById('creditModal');
    const personModalEl = document.getElementById('personModal');
    const creditModal = creditModalEl ? new bootstrap.Modal(creditModalEl) : null;
    const personModal = personModalEl ? new bootstrap.Modal(personModalEl) : null;

    const creditType = document.getElementById('credit-type');
    const characterInput = document.getElementById('credit-character');
    const orderInput = document.getElementById('credit-order');
    const departmentInput = document.getElementById('credit-department');
    const jobInput = document.getElementById('credit-job');
    const castFields = document.getElementById('credit-cast-fields');
    const crewFields = document.getElementById('credit-crew-fields');
    const creditError = document.getElementById('credit-modal-error');
    const personSearchInput = document.getElementById('credit-person-search');
    const personResults = document.getElementById('credit-person-results');
    const selectedPersonBox = document.getElementById('credit-selected-person');
    const selectedPersonName = document.getElementById('credit-selected-person-name');
    const selectedPersonImage = document.getElementById('credit-selected-person-image');

    const personError = document.getElementById('person-modal-error');

    let credits = [];
    let activeTab = 'Cast';
    let editingIndex = null;
    let selectedPerson = null;
    let currentPerson = null;

    try {
        const parsed = JSON.parse(initialJson?.textContent || '[]');
        credits = Array.isArray(parsed) ? parsed : [];
    } catch {
        credits = [];
    }

    credits = credits.map(credit => ({
        personId: credit.personId || '',
        name: credit.name || '',
        profileUrl: credit.profileUrl || '',
        type: credit.type === 'Crew' ? 'Crew' : 'Cast',
        character: credit.character || '',
        department: credit.department || '',
        job: credit.job || '',
        order: credit.order ?? null
    }));

    if (serverInputs) serverInputs.innerHTML = '';

    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        })[char]);
    }

    function normalize(value) {
        return String(value ?? '').trim().toLowerCase();
    }

    function creditKey(credit) {
        return credit.type === 'Cast'
            ? `Cast|${normalize(credit.personId)}|${normalize(credit.character)}`
            : `Crew|${normalize(credit.personId)}|${normalize(credit.department)}|${normalize(credit.job)}`;
    }

    function sortCredits() {
        credits.sort((first, second) => {
            if (first.type !== second.type) return first.type === 'Cast' ? -1 : 1;

            if (first.type === 'Cast') {
                const firstOrder = first.order === null ? Number.MAX_SAFE_INTEGER : Number(first.order);
                const secondOrder = second.order === null ? Number.MAX_SAFE_INTEGER : Number(second.order);

                if (firstOrder !== secondOrder) return firstOrder - secondOrder;
                return normalize(first.name).localeCompare(normalize(second.name), 'pt-BR');
            }

            return normalize(first.department).localeCompare(normalize(second.department), 'pt-BR')
                || normalize(first.job).localeCompare(normalize(second.job), 'pt-BR')
                || normalize(first.name).localeCompare(normalize(second.name), 'pt-BR');
        });
    }

    function creditSummary(credit) {
        if (credit.type === 'Crew') {
            return `${credit.department || '—'} • ${credit.job || '—'}`;
        }

        const character = `Personagem: ${credit.character || '—'}`;
        return credit.order === null ? character : `${character} • Ordem: ${credit.order}`;
    }

    function buildRow(credit, index, visible) {
        const avatar = credit.profileUrl
            ? `<img src="${escapeHtml(credit.profileUrl)}" alt="" class="credit-row-avatar" />`
            : '<span class="credit-row-avatar credit-row-avatar-placeholder"><i class="fa-solid fa-user"></i></span>';

        const hiddenInputs = [
            ['personId', credit.personId],
            ['type', credit.type],
            ['character', credit.character],
            ['department', credit.department],
            ['job', credit.job],
            ['order', credit.order ?? '']
        ].map(([name, value]) =>
            `<input type="hidden" name="credits[${index}].${name}" value="${escapeHtml(value)}" />`)
            .join('');

        return `
            <div class="credit-row d-flex align-items-center justify-content-between gap-2 border-bottom py-2${visible ? '' : ' is-hidden'}"
                 data-credit-type="${credit.type}"
                 data-credit-index="${index}">
                <div class="d-flex align-items-center gap-2">
                    ${avatar}
                    <div>
                        <button type="button"
                                class="btn btn-link p-0 credit-person-link"
                                data-person-id="${escapeHtml(credit.personId)}">
                            ${escapeHtml(credit.name || 'Pessoa sem nome')}
                        </button>
                        <div class="small text-muted">${escapeHtml(creditSummary(credit))}</div>
                    </div>
                </div>

                <div class="d-flex gap-2">
                    <button type="button" class="btn btn-sm btn-outline-primary btn-edit-credit" title="Editar">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-danger btn-remove-credit" title="Remover">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>

                ${hiddenInputs}
            </div>`;
    }

    function updateCounts() {
        document.querySelectorAll('[data-credit-count]').forEach(element => {
            const type = element.dataset.creditCount;
            element.textContent = credits.filter(credit => credit.type === type).length;
        });

        const totalBadge = document.getElementById('credits-count-badge');
        if (totalBadge) totalBadge.textContent = credits.length;
    }

    function applyTabFilter() {
        listEl.querySelectorAll('.credit-row').forEach(row => {
            row.classList.toggle('is-hidden', row.dataset.creditType !== activeTab);
        });
    }

    function updateEmptyState() {
        const visibleCredits = credits.filter(credit => credit.type === activeTab);
        emptyEl.classList.toggle('d-none', visibleCredits.length > 0);
    }

    function renderCredits() {
        sortCredits();
        listEl.innerHTML = credits.map((credit, index) => buildRow(credit, index, credit.type === activeTab)).join('');
        updateEmptyState();
        updateCounts();
    }

    function setActiveTab(tab) {
        activeTab = tab === 'Crew' ? 'Crew' : 'Cast';

        document.querySelectorAll('[data-credit-tab]').forEach(button => {
            button.classList.toggle('active', button.dataset.creditTab === activeTab);
        });

        applyTabFilter();
        updateEmptyState();
    }

    function showCreditError(message) {
        creditError.textContent = message;
        creditError.classList.remove('d-none');
    }

    function clearCreditError() {
        creditError.textContent = '';
        creditError.classList.add('d-none');
    }

    function showPersonError(message) {
        personError.textContent = message;
        personError.classList.remove('d-none');
    }

    function clearPersonError() {
        personError.textContent = '';
        personError.classList.add('d-none');
    }

    function setCreditType(type) {
        creditType.value = type === 'Crew' ? 'Crew' : 'Cast';
        castFields.classList.toggle('d-none', creditType.value !== 'Cast');
        crewFields.classList.toggle('d-none', creditType.value !== 'Crew');
    }

    function setSelectedPerson(person) {
        selectedPerson = person;

        if (!person) {
            selectedPersonBox.classList.add('d-none');
            return;
        }

        selectedPersonName.textContent = person.name || '';
        selectedPersonImage.src = person.profileUrl || '';
        selectedPersonImage.classList.toggle('d-none', !person.profileUrl);
        selectedPersonBox.classList.remove('d-none');
    }

    function resetCreditModal() {
        editingIndex = null;
        setSelectedPerson(null);

        creditType.disabled = false;
        characterInput.value = '';
        orderInput.value = '';
        departmentInput.value = '';
        jobInput.value = '';
        personSearchInput.value = '';
        personResults.innerHTML = '';
        personResults.classList.add('d-none');

        setCreditType(activeTab);
        clearCreditError();

        document.getElementById('creditModalTitle').textContent = 'Adicionar crédito';
    }

    function openCreditModal(index) {
        resetCreditModal();

        if (index !== null && index !== undefined) {
            editingIndex = index;
            const credit = credits[index];

            setCreditType(credit.type);
            characterInput.value = credit.character || '';
            orderInput.value = credit.order ?? '';
            departmentInput.value = credit.department || '';
            jobInput.value = credit.job || '';

            setSelectedPerson({
                personId: credit.personId,
                name: credit.name,
                profileUrl: credit.profileUrl
            });

            document.getElementById('creditModalTitle').textContent = 'Editar crédito';
        }

        creditModal.show();
    }

    async function postForm(url, payload) {
        const body = new URLSearchParams();

        Object.entries(payload).forEach(([key, value]) => {
            if (value !== undefined && value !== null) body.append(key, value);
        });

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body
        });

        const text = await response.text();

        if (!response.ok) throw new Error(text || 'Não foi possível concluir a operação.');
        return text ? JSON.parse(text) : null;
    }

    async function postJson(url, payload) {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const text = await response.text();

        if (!response.ok) throw new Error(text || 'Não foi possível concluir a operação.');
        return text ? JSON.parse(text) : null;
    }

    async function getJson(url) {
        const response = await fetch(url);
        const text = await response.text();

        if (!response.ok) throw new Error(text || 'Não foi possível carregar os dados.');
        return text ? JSON.parse(text) : null;
    }

    async function searchPeople() {
        const term = personSearchInput.value.trim();

        clearCreditError();
        personResults.classList.add('d-none');
        personResults.innerHTML = '';

        if (term.length < 2) {
            showCreditError('Digite ao menos 2 caracteres para pesquisar.');
            return;
        }

        personResults.classList.remove('d-none');
        personResults.innerHTML = '<div class="list-group-item text-muted small">Pesquisando no TMDB...</div>';

        try {
            const results = await getJson(`/People/SearchTmdb?name=${encodeURIComponent(term)}`);

            if (!Array.isArray(results) || results.length === 0) {
                personResults.innerHTML = '<div class="list-group-item text-muted small">Nenhuma pessoa encontrada.</div>';
                return;
            }

            personResults.innerHTML = results.map(person => {
                const image = person.profileUrl
                    ? `<img src="${escapeHtml(person.profileUrl)}" alt="" class="credit-row-avatar" />`
                    : '<span class="credit-row-avatar credit-row-avatar-placeholder"><i class="fa-solid fa-user"></i></span>';

                return `
                    <button type="button"
                            class="list-group-item list-group-item-action d-flex align-items-center justify-content-between gap-2 credit-person-result"
                            data-local-id="${escapeHtml(person.localId || '')}"
                            data-tmdb-id="${escapeHtml(person.tmdbId)}"
                            data-name="${escapeHtml(person.name)}"
                            data-profile-url="${escapeHtml(person.profileUrl || '')}">
                        <span class="d-flex align-items-center gap-2">
                            ${image}
                            <span>${escapeHtml(person.name)}</span>
                        </span>
                        <span class="badge ${person.isRegistered ? 'text-bg-success' : 'text-bg-secondary'}">
                            ${person.isRegistered ? 'Já cadastrada' : 'Importar'}
                        </span>
                    </button>`;
            }).join('');
        } catch (error) {
            personResults.innerHTML = `<div class="list-group-item text-danger small">${escapeHtml(error.message)}</div>`;
        }
    }

    async function selectPersonResult(button) {
        const localId = button.dataset.localId;
        const name = button.dataset.name;
        const profileUrl = button.dataset.profileUrl;

        if (localId) {
            setSelectedPerson({ personId: localId, name, profileUrl });
            personResults.classList.add('d-none');
            return;
        }

        button.disabled = true;

        try {
            const person = await postForm('/People/ImportPerson', { tmdbId: button.dataset.tmdbId });

            setSelectedPerson({
                personId: person.id,
                name: person.name,
                profileUrl: person.profileUrl
            });

            personResults.classList.add('d-none');
        } catch (error) {
            showCreditError(error.message);
        } finally {
            button.disabled = false;
        }
    }

    function saveCredit() {
        clearCreditError();

        if (!selectedPerson) {
            showCreditError('Selecione uma pessoa para o crédito.');
            return;
        }

        const type = creditType.value;
        const character = characterInput.value.trim();
        const department = departmentInput.value.trim();
        const job = jobInput.value.trim();
        const rawOrder = orderInput.value.trim();
        const parsedOrder = rawOrder === '' ? null : Number(rawOrder);
        const order = Number.isFinite(parsedOrder) ? parsedOrder : null;

        if (type === 'Cast' && !character) {
            showCreditError('Informe o personagem do crédito de elenco.');
            return;
        }

        if (type === 'Crew' && (!department || !job)) {
            showCreditError('Informe departamento e função do crédito de equipe.');
            return;
        }

        const candidate = {
            personId: selectedPerson.personId,
            name: selectedPerson.name,
            profileUrl: selectedPerson.profileUrl,
            type,
            character: type === 'Cast' ? character : '',
            department: type === 'Crew' ? department : '',
            job: type === 'Crew' ? job : '',
            order: type === 'Cast' ? order : null
        };

        const candidateKey = creditKey(candidate);
        const isDuplicated = credits.some((credit, index) =>
            index !== editingIndex && creditKey(credit) === candidateKey);

        if (isDuplicated) {
            showCreditError('Já existe um crédito equivalente para esta pessoa nesta obra.');
            return;
        }

        if (editingIndex === null) {
            credits.push(candidate);
        } else {
            credits[editingIndex] = candidate;
        }

        creditModal.hide();
        renderCredits();
    }

    function fillPersonForm(person) {
        currentPerson = person;

        document.getElementById('person-id').value = person.id || '';
        document.getElementById('person-tmdb-id').value = person.tmdbId ?? '';
        document.getElementById('person-name').value = person.name || '';
        document.getElementById('person-imdb-id').value = person.imdbId || '';
        document.getElementById('person-birthday').value = person.birthday ? String(person.birthday).substring(0, 10) : '';
        document.getElementById('person-deathday').value = person.deathday ? String(person.deathday).substring(0, 10) : '';
        document.getElementById('person-place-of-birth').value = person.placeOfBirth || '';
        document.getElementById('person-profile-url').value = person.profileUrl || '';
        document.getElementById('person-biography').value = person.biography || '';

        const image = document.getElementById('person-profile-image');
        image.src = person.profileUrl || '';
        image.classList.toggle('d-none', !person.profileUrl);

        clearPersonError();
    }

    function updateCreditsPerson(person) {
        let changed = false;

        credits.forEach(credit => {
            if (credit.personId !== person.id) return;

            credit.name = person.name;
            credit.profileUrl = person.profileUrl || credit.profileUrl;
            changed = true;
        });

        if (changed) renderCredits();
    }

    async function openPersonModal(personId) {
        if (!personId) return;

        try {
            const person = await getJson(`/People/Get?id=${encodeURIComponent(personId)}`);
            fillPersonForm(person);
            personModal.show();
        } catch (error) {
            window.alert(error.message);
        }
    }

    async function savePerson() {
        clearPersonError();

        const name = document.getElementById('person-name').value.trim();

        if (!name) {
            showPersonError('Informe o nome da pessoa.');
            return;
        }

        const tmdbId = document.getElementById('person-tmdb-id').value.trim();

        try {
            const person = await postForm('/People/Update', {
                id: document.getElementById('person-id').value,
                tmdbId,
                imdbId: document.getElementById('person-imdb-id').value.trim(),
                name,
                biography: document.getElementById('person-biography').value,
                birthday: document.getElementById('person-birthday').value,
                deathday: document.getElementById('person-deathday').value,
                placeOfBirth: document.getElementById('person-place-of-birth').value,
                profileUrl: document.getElementById('person-profile-url').value.trim()
            });

            fillPersonForm(person);
            updateCreditsPerson(person);
            personModal.hide();
        } catch (error) {
            showPersonError(error.message);
        }
    }

    async function refreshPerson() {
        clearPersonError();

        if (!currentPerson?.id) {
            showPersonError('Pessoa inválida para enriquecimento.');
            return;
        }

        try {
            const person = await postForm('/People/RefreshFromTmdb', { id: currentPerson.id });
            fillPersonForm(person);
            updateCreditsPerson(person);
        } catch (error) {
            showPersonError(error.message);
        }
    }

    function updateImportButton() {
        const hasImdbId = Boolean(imdbInput?.value.trim());
        importButton.disabled = !hasImdbId;

        importHint.textContent = hasImdbId
            ? 'A importação usa o IMDb ID preenchido na etapa anterior.'
            : 'Preencha o IMDb ID na etapa anterior.';
    }

    function toRequestCredit(credit) {
        return {
            personId: credit.personId,
            type: credit.type === 'Crew' ? 1 : 0,
            character: credit.character || '',
            department: credit.department || '',
            job: credit.job || '',
            order: credit.order
        };
    }

    async function importCredits() {
        if (!controller) return;

        const imdbId = imdbInput?.value.trim();

        if (!imdbId) {
            importFeedback.className = 'small mb-2 text-danger';
            importFeedback.textContent = 'Preencha o IMDb ID da obra para importar o elenco e a equipe.';
            return;
        }

        const workId = form?.querySelector('input[name="id"]')?.value || '';
        const workTitle = document.getElementById('title')?.value || '';

        importButton.disabled = true;
        importFeedback.className = 'small mb-2 text-muted';
        importFeedback.textContent =
            'Importando elenco, equipe e dados das pessoas do TMDB. Isso pode levar alguns segundos...';

        try {
            const response = await postJson(`/${controller}/ImportCredits`, {
                imdbId,
                workId,
                workTitle,
                credits: credits.map(toRequestCredit)
            });

            credits = (response.credits || []).map(credit => ({
                personId: credit.personId || '',
                name: credit.name || '',
                profileUrl: credit.profileUrl || '',
                type: credit.type === 'Crew' ? 'Crew' : 'Cast',
                character: credit.character || '',
                department: credit.department || '',
                job: credit.job || '',
                order: credit.order ?? null
            }));

            renderCredits();

            importFeedback.className = 'small mb-2 text-success';
            importFeedback.textContent =
                `${response.imported} crédito(s) importado(s), ${response.ignored} ignorado(s), ` +
                `${response.peopleCreated} pessoa(s) criada(s) e ${response.peopleReused} reutilizada(s).`;
        } catch (error) {
            importFeedback.className = 'small mb-2 text-danger';
            importFeedback.textContent = error.message;
        } finally {
            updateImportButton();
        }
    }

    document.querySelectorAll('[data-credit-tab]').forEach(button => {
        button.addEventListener('click', () => setActiveTab(button.dataset.creditTab));
    });

    document.getElementById('btn-add-credit')?.addEventListener('click', () => openCreditModal(null));
    document.getElementById('btn-search-person')?.addEventListener('click', searchPeople);

    personSearchInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            searchPeople();
        }
    });

    personResults?.addEventListener('click', event => {
        const button = event.target.closest('.credit-person-result');
        if (button) selectPersonResult(button);
    });

    document.getElementById('btn-clear-selected-person')?.addEventListener('click', () => {
        setSelectedPerson(null);
        personSearchInput.value = '';
    });

    creditType?.addEventListener('change', () => setCreditType(creditType.value));
    document.getElementById('btn-save-credit')?.addEventListener('click', saveCredit);

    listEl?.addEventListener('click', event => {
        const personLink = event.target.closest('.credit-person-link');

        if (personLink) {
            openPersonModal(personLink.dataset.personId);
            return;
        }

        const row = event.target.closest('.credit-row');
        if (!row) return;

        const index = Number(row.dataset.creditIndex);

        if (event.target.closest('.btn-edit-credit')) {
            openCreditModal(index);
        }

        if (event.target.closest('.btn-remove-credit')) {
            credits.splice(index, 1);
            renderCredits();
        }
    });

    document.getElementById('btn-save-person')?.addEventListener('click', savePerson);
    document.getElementById('btn-refresh-person')?.addEventListener('click', refreshPerson);

    importButton?.addEventListener('click', importCredits);
    imdbInput?.addEventListener('input', updateImportButton);

    updateImportButton();
    renderCredits();
})();
