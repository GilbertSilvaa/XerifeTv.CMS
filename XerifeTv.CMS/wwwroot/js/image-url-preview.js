(() => {
    const previewModalId = 'imageUrlPreviewModal';

    function getValidImageUrl(value) {
        try {
            const url = new URL(value.trim());
            return ['http:', 'https:'].includes(url.protocol) ? url.href : null;
        }
        catch {
            return null;
        }
    }

    function updateButton(input) {
        const button = input.parentElement?.querySelector('.image-url-preview-button');
        if (button) button.disabled = !getValidImageUrl(input.value);
    }

    function initializeInput(input) {
        if (input.dataset.imageUrlPreviewInitialized) return;

        input.dataset.imageUrlPreviewInitialized = 'true';
        const group = document.createElement('div');
        group.className = 'input-group';
        input.parentNode.insertBefore(group, input);
        group.appendChild(input);

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-outline-secondary image-url-preview-button shadow-sm';
        button.title = 'Visualizar imagem';
        button.setAttribute('aria-label', 'Visualizar imagem');
        button.innerHTML = '<i class="fa-solid fa-image" aria-hidden="true"></i>';
        group.appendChild(button);

        $(input).on('input change', () => updateButton(input));
        button.addEventListener('click', () => showPreview(input));
        updateButton(input);
    }

    function initializeInputs(root = document) {
        root.querySelectorAll('input[data-image-url-preview]').forEach(initializeInput);
    }

    function showPreview(input) {
        const imageUrl = getValidImageUrl(input.value);
        if (!imageUrl) return;

        const modal = document.getElementById(previewModalId);
        const image = modal.querySelector('img');
        image.src = imageUrl;
        image.alt = input.labels[0]?.textContent.trim() || 'Pré-visualização da imagem';
        bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    document.addEventListener('DOMContentLoaded', () => initializeInputs());
})();
