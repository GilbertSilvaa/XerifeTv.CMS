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
      }, 20000);
    }, 10000);
  }, 5000);

  return [
    progressAction1TimeOut,
    progressAction2TimeOut,
    progressAction3TimeOut
  ];
}

// submit spreadsheet
$('.btn-excel-file-submit').on('click', async function (){
  if (!confirm('Confirmar ação?')) return;

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
    $('.importFromExcelModal .btn-close').hide();
    $('.process-file-container').show();

    const response = await fetch(`/${controller}/${action}`, {
      method: 'POST',
      body: formData
    });

    if (!response.ok) throw await response.text();
    const { successCount, failCount, errorList } = await response.json();

    $('.finish-process-container .success-count').text(successCount);
    $('.finish-process-container .fail-count').text(failCount);

    $(errorList).each((index, message) => {
      const errorItem = document.createElement('li');
      errorItem.textContent = message;
      errorItem.classList.add('list-group-item');
      $('.finish-process-container .errorList .list-group').append(errorItem);
    });

    if (errorList.length > 0) $('.finish-process-container .errorList').show();
  }
  catch (error) {
    if (!error) return;
    const errorItem = document.createElement('li');
    errorItem.textContent = String(error);
    errorItem.classList.add('list-group-item');
    $('.finish-process-container .errorList .list-group').append(errorItem);
    $('.finish-process-container .errorList').show();
  }
  finally {
    progressBarEmulateTimeOuts.forEach(timeOut => clearTimeout(timeOut));

    $('.process .progress-bar').css('width', '100%');
    $('.process span.status-percent').text('100%');
    $('.process span.status-text').text('Processo de cadastros finalizado.');

    setTimeout(() => {
      $('.process-file-container').hide();
      $('.finish-process-container').show();

      $(btn).text('Pronto').prop('disabled', false);
      $(btn).off().click(() => location.replace(`/${controller}`));
    }, 1250);
  }
});