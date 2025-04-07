window.onload =  () => {
  const theme = localStorage.getItem('theme');
  
  // check if dark theme
  if (theme === 'dark') {
    const darkThemeCheckbox = $('#darkTheme');
    $(darkThemeCheckbox).prop('checked', true);
    $(darkThemeCheckbox).parent().siblings('i.fa-sun').removeClass('text-warning');
    $(darkThemeCheckbox).parent().siblings('i.fa-moon').addClass('text-warning');
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
  $('#darkTheme').on('change', function (){
    const isDarkTheme = $(this).prop('checked');
    
    $('html').attr('data-bs-theme', isDarkTheme ? 'dark' : 'light');
    localStorage.setItem('theme', isDarkTheme ? 'dark' : 'light');
    
    if (isDarkTheme) {
      $('body').addClass('bg-dark');
      $(this).parent().siblings('i.fa-sun').removeClass('text-warning');
      $(this).parent().siblings('i.fa-moon').addClass('text-warning');
    }
    else {
      $('body').removeClass('bg-dark');
      $(this).parent().siblings('i.fa-moon').removeClass('text-warning');
      $(this).parent().siblings('i.fa-sun').addClass('text-warning');
    }
  });
});