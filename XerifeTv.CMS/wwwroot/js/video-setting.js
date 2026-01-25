(function (global, $) {
    if (!global) return;

    function uploadSubtitleFile(fileInputId, subtitleInputId, btnId) {
        const fileInput = document.getElementById(fileInputId);
        if (!fileInput || !fileInput.files || !fileInput.files[0]) return;

        const file = fileInput.files[0];
        const formData = new FormData();
        formData.append('file', file);

        const $btn = $(`#${btnId}`);
        $btn.attr('disabled', 'true');
        $('.loading-global .label').text('Processando upload, por favor aguarde...');
        $('.loading-global').show();

        fetch('/StorageFiles/UploadFile', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(({ isSuccess, data }) => {
                if (isSuccess) $(`#${subtitleInputId}`).val(data);
            })
            .finally(() => {
                $btn.removeAttr('disabled');
                $('.loading-global').hide();
            });
    }

    function initVideoSetting(prefix) {
        if (!window.jQuery) {
            setTimeout(() => initVideoSetting(prefix), 50);
            return;
        }

        const $ = window.jQuery;
        const $videoUrl = $(`#${prefix}_videoUrl`);
        if ($videoUrl.length === 0) return;

        const $form = $videoUrl.closest('form');

        $(`#${prefix}_btn_subtitle_file`).off(`click.vs`).on('click.vs', function () {
            $(`#${prefix}_subtitle_file`).click();
        });

        $(`#${prefix}_subtitle_file`).off(`change.vs`).on('change.vs', function () {
            uploadSubtitleFile(`${prefix}_subtitle_file`, `${prefix}_videoSubtitle`, `${prefix}_btn_subtitle_file`);
        });

        $(`#${prefix}_videoSourceType`).off(`change.vs_${prefix}`).on(`change.vs_${prefix}`, function () {
            const $this = $(this);
            const $thisForm = $this.closest('form');
            if ($thisForm.find(`#${prefix}_videoUrl`).length === 0) return;

            const value = $this.val();
            if (value === 'delivery') {
                $(`#${prefix}_video-direct-section`).addClass('d-none');
                $(`#${prefix}_video-delivery-section`).removeClass('d-none');

                $(`#${prefix}_videoUrl`).val('');
                $(`#${prefix}_videoStreamFormat`).val('');

                $(`#${prefix}_mediaDeliveryProfileId`).prop('required', true);
                $(`#${prefix}_mediaRoute`).prop('required', true);
                $(`#${prefix}_videoUrl`).prop('required', false);
                $(`#${prefix}_videoStreamFormat`).prop('required', false);
            } else {
                $(`#${prefix}_video-direct-section`).removeClass('d-none');
                $(`#${prefix}_video-delivery-section`).addClass('d-none');

                $(`#${prefix}_mediaDeliveryProfileId`).val('');
                $(`#${prefix}_mediaRoute`).val('');

                $(`#${prefix}_videoUrl`).prop('required', true);
                $(`#${prefix}_videoStreamFormat`).prop('required', true);
                $(`#${prefix}_mediaDeliveryProfileId`).prop('required', false);
                $(`#${prefix}_mediaRoute`).prop('required', false);
            }
        });
    }

    global.initVideoSetting = initVideoSetting;

})(window, window.jQuery);