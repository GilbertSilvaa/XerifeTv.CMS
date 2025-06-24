// upload excel file 
$('.importExcelFile').on('change', function () {
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

// submit spreadsheet
$('.btn-excel-file-submit').on('click', async function () {
  if (!confirm('Confirmar ação?')) return;

  const btn = this;
  const file = $('.importExcelFile').prop('files')[0];
  if (!file) return;

  const formData = new FormData();
  formData.append('file', file);

  const controller = $(btn).data('controller');
  const action = $(btn).data('action');
  const actionMonitorProgress = $(btn).data('monitorProgressAction');

  var monitorProgressInterval = 0;

  try {
    $(btn).text('Processando...').prop('disabled', true);
    $('.isBackgroundJob').prop('disabled', true);

    if ($('.isBackgroundJob').is(':checked')) {
      const formDataBackgroundJob = new FormData();
      formDataBackgroundJob.append('spreadsheetFile', file);

      const controllerType = controller.toUpperCase();
      const backgroundJobTypes = {
        SERIES: 'REGISTER_SPREADSHEET_SERIES',
        CHANNELS: 'REGISTER_SPREADSHEET_CHANNELS',
        MOVIES: 'REGISTER_SPREADSHEET_MOVIES',
      };

      const backgroundJobType = backgroundJobTypes[controllerType] || 'REGISTER_SPREADSHEET_MOVIES';

      formDataBackgroundJob.append('type', backgroundJobType);

      await fetch(`/BackgroundJobQueue/AddJobInQueueSpreadsheetRegisters`, {
        method: 'POST',
        body: formDataBackgroundJob
      });

      location.replace(`/${controller}`);
      return;
    }

    $('.isBackgroundJob').parent().hide();
    $('.select-file-container').hide();
    $('.importFromExcelModal .btn-close').hide();
    $('.process-file-container').show();

    // submit file
    const response = await fetch(`/${controller}/${action}`, {
      method: 'POST',
      body: formData
    });

    const importId = await response.text();

    // monitor progress records
    monitorProgressInterval = setInterval(async () => {

      var monitorResponse = await fetch(`/${controller}/${actionMonitorProgress}?importId=${importId}`);
      const { successCount, failCount, errorList, progressCount } = await monitorResponse.json();

      if (progressCount == 0) return;

      $('.process .progress-bar').css('width', `${progressCount}%`);
      $('.process span.status-percent').text(`${progressCount}%`);

      if (progressCount == 100) {
        clearInterval(monitorProgressInterval);

        $('.finish-process-container .success-count').text(successCount);
        $('.finish-process-container .fail-count').text(failCount);

        $(errorList).each((index, message) => {
          const errorItem = document.createElement('li');
          errorItem.textContent = message;
          errorItem.classList.add('list-group-item');
          $('.finish-process-container .errorList .list-group').append(errorItem);
        });

        if (errorList.length > 0) $('.finish-process-container .errorList').show();

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

    }, 2500);
  }
  catch (error) {
    if (!error) return;
    const errorItem = document.createElement('li');
    errorItem.textContent = String(error);
    errorItem.classList.add('list-group-item');

    $('.finish-process-container .errorList .list-group').append(errorItem);
    $('.finish-process-container .errorList').show();

    clearInterval(monitorProgressInterval);
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