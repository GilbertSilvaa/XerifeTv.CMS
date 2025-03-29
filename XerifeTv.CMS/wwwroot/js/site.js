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
  
  // upload excel file 
  $('.importExcelFile').on('change', function (){
    const file = $(this).prop('files')[0];
    if (!file) return;
    
    $('.file-uploaded-name i').addClass('fa-solid fa-file-excel');
    $('.file-uploaded-name span').text(file.name);
    $('.btn-excel-file-submit').prop('disabled', false);
  });

  // when closing the modal reset form
  $('.importFromExcelModal').on('hidden.bs.modal', () => {
    $('.importExcelFile').val('');
    $('.file-uploaded-name i').removeClass('fa-solid fa-file-excel');
    $('.file-uploaded-name span').text('');
    $('.btn-excel-file-submit').text('Cadastrar').prop('disabled', true);
  });

  // emulates the progress of the progress bar
  const emulateProgressBarAction = () => {
    let [
      progressAction1TimeOut,
      progressAction2TimeOut,
      progressAction3TimeOut
    ] = [0, 0, 0];

    progressAction1TimeOut = setTimeout(() => {
      $('.process .progress-bar').css('width', '25%');
      $('.process span.status-percent').text('25%');

      progressAction2TimeOut = setTimeout(() => {
        $('.process .progress-bar').css('width', '55%');
        $('.process span.status-percent').text('55%');

        progressAction3TimeOut = setTimeout(() => {
          $('.process .progress-bar').css('width', '80%');
          $('.process span.status-percent').text('80%');
        }, 6000);
      }, 6000);
    }, 5000);

    return [
      progressAction1TimeOut,
      progressAction2TimeOut,
      progressAction3TimeOut
    ];
  }
  
  // submit spreadsheet
  $('.btn-excel-file-submit').on('click', async function (){
    const btn = this;
    
    const file = $('.importExcelFile').prop('files')[0];
    if (!file) return;
    
    const formData = new FormData();
    formData.append('file', file);
    
    const [controller, action] = [$(btn).data('controller'), $(btn).data('action')];
    const progressBarEmulateTimeOuts =  emulateProgressBarAction();
    
    try {
      $(btn).text('Processando...').prop('disabled', true);
      $('.select-file-container').hide();
      $('.process-file-container').show();
      $('.importFromExcelModal .btn-close').prop('disabled', true);
      
      const response = await fetch(`/${controller}/${action}`, {
        method: 'POST',
        body: formData
      });
      
      const data = await response.json();
      console.log(data);

      $('.process .progress-bar').css('width', '100%');
      $('.process span.status-percent').text('100%');
      $('.process span.status-text').text('Processo de cadastros finalizado.');
      
      setTimeout(() => {
        $('.process-file-container').hide();
        $('.finish-process-container').show();
      }, 2000);
    }
    catch (error) {
      console.log(error);
    }
    finally {
      progressBarEmulateTimeOuts.forEach(timeOut => clearTimeout(timeOut));
    }
  });
});