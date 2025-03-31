window.onload =  () => {
  const theme = localStorage.getItem('theme');
  
  // check if dark theme
  if (theme === 'dark') {
    $('#change-theme').data('theme', 'light');
    $('#change-theme').find('i').removeClass('fa-moon');
    $('#change-theme').find('i').addClass('fa-sun');
  }
}

document.addEventListener('DOMContentLoaded', function() {
  const forms = document.querySelectorAll('form');

  // keeps form fields and buttons disabled during the submit
  forms.forEach(form => {
    if (form.method.toUpperCase() === 'POST') {
      const elements = form.querySelectorAll('input, select, button, textarea');

      form.addEventListener('submit', function() {
        elements.forEach(element => {
          element.tagName.toLowerCase() === 'button'
            ? element.disabled = true
            : element.setAttribute('readonly', true);
        });
      });
    }
  });
  
  // change global theme
  $('#change-theme').on('click', function (){
    const theme = $(this).data('theme');
    $('html').attr('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
    
    if (theme === 'dark') {
      $('body').addClass('bg-dark');
      $(this).data('theme', 'light');
      $(this).find('i').removeClass('fa-moon');
      $(this).find('i').addClass('fa-sun');
      $('body').addClass('bg-dark');
    }
    else {
      $('body').removeClass('bg-dark');
      $(this).data('theme', 'dark');
      $(this).find('i').removeClass('fa-sun');
      $(this).find('i').addClass('fa-moon');
    }
  });
});