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
  
  // forms with disabled fields that can be enabled with an edit/save button
  $('.unblock-form').on('click', function (event){
    if ($(this).prop('type') !== 'submit') {
      event.preventDefault();
      $(this).parent('form').find('input[disabled]').prop('disabled', false);
      $(this).parent('form').find('input[type!=hidden]').first().trigger('focus');
      $(this).text($(this).data('title'));
      $(this).prop('type', 'submit');
    }
  });
  
  // forms that require confirmation to be submitted
  $('form[data-confirm]').on('submit', function (event) {
    event.preventDefault();
    
    if (confirm($(this).data('confirm'))) {
      $(this).off('submit').trigger('submit');
      return;
    }
      
    $(this).find('input, select, button, textarea')
      .prop('disabled', false)
      .prop('readonly', false);
  });

  // inputs hidden when requireds
  $('form:not([data-confirm])').on('submit', function (event) { 
    event.preventDefault();
    var isFormValid = true;
    
    $(this).find('input[type=hidden]').each(function () {
      if ($(this).prop('required') && $(this).val() === '') {
        alert($(this).data('requiredMessage'));
        isFormValid = false;
      }
    })

    if (isFormValid) {
      $(this).off('submit').trigger('submit');
      return;
    }

    $(this).find('input, select, button, textarea')
      .prop('disabled', false)
      .prop('readonly', false);
  })
  
  // toggles input visibility (show/hide) when icon button is clicked
  $('.show-hide-password').on('click', function (event) {
    const input = $(this).siblings('input');
    
    if ($(input).prop('type') === 'password') {
      $(input).prop('type', 'text');
      $(this).find('i').removeClass('fa-regular fa-eye-slash');
      $(this).find('i').addClass('fa-regular fa-eye');
    }
    else {
      $(input).prop('type', 'password');
      $(this).find('i').removeClass('fa-regular fa-eye');
      $(this).find('i').addClass('fa-regular fa-eye-slash');
    }
  });
  
  // input tags/categories
  $('.input-tags').on('keydown blur', function (event) {
    if (event.key !== 'Enter' && event.key !== ',' && event.key !== ';' && event.type !== 'blur') return;
    
    if ($(this).val() === '') {
      setTimeout(() => $(this).val(''), 1);
      return;
    }

    $(this).val().split(',').forEach(tag => {
      const tagsValue = $(this).siblings('.input-tags-value ').val();

      if (tagsValue.indexOf(tag) > -1) {
        setTimeout(() => $(this).val(''), 1);
        return;
      }

      const positionTagIndex = tagsValue.split(',').length;

      $(this).siblings('.container-tags ').append(`
        <div class="rounded-1 bg-secondary text-light py-0 px-2 d-flex justify-content-between align-items-center gap-2">
          <span class="fw-normal text-nowrap">${ tag.substring(0, 30) }</span>
          <button
            type="button"
            class="btn-close shadow-none btn-remove-tag"
            aria-label="Close"
            data-position="${positionTagIndex}"
            style="width: 4px; height: 4px;">
          </button>
        </div>
      `);

      $(`.btn-remove-tag[data-position="${positionTagIndex}"]`).click(function () {
        const $btn = $(this);
        const $tags = $(this).parent().parent('.container-tags').find('div');
        var tagsValuesResult = '';

        const currentTagText = $($btn).siblings('span.fw-normal').text().trim();
        $tags.each(function () {
          if ($(this).find('span.fw-normal').text().trim() === currentTagText) return;
          tagsValuesResult += $(this).find('span.fw-normal').text() + ' ,';
        });

        $($btn).parent().parent().siblings('.input-tags-value').val(tagsValuesResult);
        $($btn).parent().remove();
      })

      $(this).siblings('.input-tags-value').val(tagsValue + `${ tag.substring(0, 30) }, `);
      setTimeout(() => $(this).val(''), 1);
    })
  })

  // populate tags input with existing values in edit mode
  $('.input-tags').trigger('blur');
});