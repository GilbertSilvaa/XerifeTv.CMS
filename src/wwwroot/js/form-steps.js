(function () {
    function activateStep(form, stepNumber, scroll) {
        const steps = form.querySelectorAll('[data-step]');
        const buttons = form.querySelectorAll('[data-step-target]');

        steps.forEach(step => {
            step.classList.toggle('active', step.dataset.step === String(stepNumber));
        });

        buttons.forEach(button => {
            const isActive = button.dataset.stepTarget === String(stepNumber);
            button.classList.toggle('active', isActive);
            button.setAttribute('aria-selected', isActive ? 'true' : 'false');
        });

        if (scroll) {
            const target = Array.from(steps)
                .find(step => step.dataset.step === String(stepNumber));

            target?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function initStepForm(form) {
        if (!form.querySelector('[data-step]')) return;

        form.querySelectorAll('[data-step-target]').forEach(button => {
            button.addEventListener('click', () => activateStep(form, button.dataset.stepTarget, false));
        });

        form.addEventListener('submit', function (event) {
            const invalidFields = Array.from(form.querySelectorAll(':invalid'));
            const invalidHiddenFields = Array.from(form.querySelectorAll('input[type="hidden"][required]'))
                .filter(input => !input.value);

            if (invalidFields.length === 0 && invalidHiddenFields.length === 0) return;

            event.preventDefault();
            event.stopImmediatePropagation();

            const firstInvalid = invalidFields[0] ?? invalidHiddenFields[0];
            const step = firstInvalid?.closest('[data-step]');

            if (step) activateStep(form, step.dataset.step, true);

            if (invalidHiddenFields.length > 0) {
                const message = invalidHiddenFields[0].dataset.requiredMessage
                    ?? 'Preencha os campos obrigatórios.';

                window.alert(message);
            }

            const tagInput = invalidHiddenFields
                .map(input => input.closest('.input-group-tags')?.querySelector('input.input-tags'))
                .find(Boolean);

            const focusable = invalidFields.find(input => input.type !== 'hidden' && !input.disabled)
                ?? tagInput;

            if (focusable) {
                setTimeout(() => {
                    focusable.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    focusable.focus({ preventScroll: true });
                }, 80);
            }
        });
    }

    document.querySelectorAll('form').forEach(initStepForm);
})();
