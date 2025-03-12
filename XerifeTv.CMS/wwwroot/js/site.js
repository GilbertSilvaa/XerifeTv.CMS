document.addEventListener('DOMContentLoaded', function() {
  const forms = document.querySelectorAll('form');

  forms.forEach(form => {
    if (form.method.toUpperCase() === 'POST') {
      const elements = form.querySelectorAll('input, select, button');

      form.addEventListener('submit', () =>
        elements.forEach(element => element.disabled = true));
    }
  });
});