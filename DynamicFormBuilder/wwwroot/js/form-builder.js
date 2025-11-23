$(function () {

    var optionSets = []; // fetched from server
    var fieldIndex = 0;

    function loadOptionSets() {
        return $.get('/api/optionsets').done(function (res) { optionSets = res; });
    }

    function loadOptionValues(optionId) {
        return $.get('/api/options/' + optionId);
    }

    function buildFieldHtml(idx) {
        var html = `<div class="field-item mb-3 p-3 border rounded" data-index="${idx}">
            <div class="row g-2">
                <div class="col-md-5">
                    <label class="form-label">Label</label> <span class="text-danger">*</span>
                    <input class="fld-label form-control" placeholder="Enter form label"/>
                    <div class="invalid-feedback"></div>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Required</label>
                    <div><input type="checkbox" class="fld-required form-check-input" /></div>
                </div>
                <div class="col-md-5">
                    <label class="form-label">Option Set</label> <span class="text-danger">*</span>
                    <select class="fld-optionset form-select">
                        <option value="">-- Choose Option Set --</option>`;
        optionSets.forEach(function (s) {
            html += `<option value="${s.optionId}">${s.name}</option>`;
        });
        html += `</select>
                    <div class="invalid-feedback"></div>
                </div>
                <div class="col-12 mt-2">
                    <label class="form-label">Options</label>
                    <select class="fld-option form-select"><option value="">-- Select --</option></select>
                    <div class="invalid-feedback"></div>
                </div>
                <div class="col-12 mt-2 text-end">
                    <button class="remove-field btn btn-sm btn-danger">Remove</button>
                </div>
            </div>
        </div>`;
        return html;
    }

    loadOptionSets().fail(function () { Swal.fire('Error', 'Failed to load option sets', 'error'); });

    $('#addField').on('click', function (e) {
        e.preventDefault();
        if (!optionSets || optionSets.length === 0) {
            Swal.fire('Error', 'No option sets available. Create from OptionSets Admin.', 'error');
            return;
        }
        $('#fieldsContainer').append(buildFieldHtml(fieldIndex));
        fieldIndex++;
    });

    $('#fieldsContainer').on('click', '.remove-field', function (e) {
        e.preventDefault();
        $(this).closest('.field-item').remove();
    });

    $('#fieldsContainer').on('change', '.fld-optionset', function () {
        var optionId = $(this).val();
        var $field = $(this).closest('.field-item');
        var $optionSelect = $field.find('.fld-option');
        $optionSelect.empty().append('<option value="">-- Select --</option>');
        if (!optionId) return;
        loadOptionValues(optionId).done(function (vals) {
            vals.forEach(function (v) {
                $optionSelect.append(`<option value="${v.optionValueId}">${v.value}</option>`);
            });
        });
    });

    // ---------------- Submit Form ----------------
    $('#submitForm').on('click', function (e) {
        e.preventDefault();

        var title = $('#formTitle').val();
        var isValid = true;

        $('.invalid-feedback').text('');
        $('#formTitle').removeClass('is-invalid');

        if (!title || title.toString().trim() === '') {
            $('#formTitle').addClass('is-invalid')
                .siblings('.invalid-feedback')
                .text('Form title is required');
            isValid = false;
        }

        var fields = [];
        $('.field-item').each(function () {
            var $this = $(this);
            var label = $this.find('.fld-label').val();
            var isReq = $this.find('.fld-required').is(':checked');
            var optionId = $this.find('.fld-optionset').val();
            var selectedOptionValueId = $this.find('.fld-option').val();

            $this.find('.fld-label, .fld-optionset, .fld-option').removeClass('is-invalid');
            $this.find('.fld-label, .fld-optionset, .fld-option').siblings('.invalid-feedback').text('');

            if (!label || label.toString().trim() === '') {
                $this.find('.fld-label').addClass('is-invalid')
                    .siblings('.invalid-feedback')
                    .text('Each field must have a label');
                isValid = false;
            }
            if (!optionId) {
                $this.find('.fld-optionset').addClass('is-invalid')
                    .siblings('.invalid-feedback')
                    .text('Each field must select an Option Set');
                isValid = false;
            }
            if (optionId && (!selectedOptionValueId || selectedOptionValueId === '')) {
                $this.find('.fld-option').addClass('is-invalid')
                    .siblings('.invalid-feedback')
                    .text('Please select an option');
                isValid = false;
            }

            fields.push({
                label: label,
                isRequired: isReq,
                optionId: optionId ? parseInt(optionId) : 0,
                selectedOptionValueId: selectedOptionValueId ? parseInt(selectedOptionValueId) : null
            });
        });

        if (!isValid) return;

        var payload = { title: title, fields: fields };

        // Disable button to prevent double submit
        $('#submitForm').prop('disabled', true);

        $.ajax({
            url: '/api/form/save',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (res) {
                // Ensure SweetAlert2 is loaded before using Swal.fire
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'success',
                        title: 'Saved!',
                        text: 'Form saved successfully.',
                        confirmButtonText: 'OK'
                    }).then(() => {
                        window.location.href = '/';
                    });
                } else {
                    alert('Form saved successfully.');
                    window.location.href = '/';
                }
            },
            error: function (xhr) {
                if (xhr.status === 409) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Duplicate Title',
                        text: xhr.responseText || "A form with this title already exists.",
                        confirmButtonText: 'OK'
                    });
                    return;
                }             
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Save failed: ' + (xhr.responseText || 'Unknown error')
                });
            },
            complete: function () {
                $('#submitForm').prop('disabled', false);
            }
        });
    });


    // ---------------- Delete Form ----------------

    $('#formsTable').on('click', '.delete-form', function () {
        const id = $(this).data('id');

        Swal.fire({
            title: 'Are you sure?',
            text: 'This form will be permanently deleted.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, Delete',
            cancelButtonText: 'Cancel'
        }).then(result => {
            if (result.isConfirmed) {
                $.ajax({
                    url: '/api/form/delete/' + id,
                    type: 'DELETE',
                    success: function () {
                        Swal.fire('Deleted!', 'Form deleted successfully.', 'success');
                        $('#formsTable').DataTable().ajax.reload();
                    },
                    error: function (xhr) {
                        Swal.fire('Error', xhr.responseText || 'Failed to delete.', 'error');
                    }
                });
            }
        });
    });

});

