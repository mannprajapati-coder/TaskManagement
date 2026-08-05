(function () {
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    function postJson(url, payload) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken
            },
            body: JSON.stringify(payload)
        }).then(function (res) {
            if (!res.ok) {
                throw new Error('Request failed with status ' + res.status);
            }
            return res.json();
        });
    }

    initStatusForm();
    initChecklist();
    initAttachmentDropzone();

    function initStatusForm() {
        var form = document.getElementById('statusForm');
        var select = document.getElementById('statusSelect');
        var statusDisplay = document.getElementById('statusDisplay');
        var feedback = document.getElementById('statusUpdateFeedback');

        if (!form || !select) {
            return;
        }

        var badgeClasses = {
            Completed: 'bg-success-subtle text-success border border-success',
            InProgress: 'bg-primary-subtle text-primary border border-primary',
            InReview: 'bg-warning-subtle text-dark border border-warning',
            Cancelled: 'bg-secondary-subtle text-secondary border border-secondary',
            Todo: 'bg-light text-dark border'
        };
        var labels = { InProgress: 'In Progress', InReview: 'In Review' };
        var taskId = form.querySelector('input[name="TaskId"]').value;

        form.addEventListener('submit', function (e) {
            e.preventDefault();

            postJson('/Task/UpdateStatusAjax', { TaskId: taskId, Status: select.value })
                .then(function (result) {
                    if (result.success) {
                        var status = result.status || select.value;
                        statusDisplay.innerHTML = '<span class="badge ' + (badgeClasses[status] || 'bg-light text-dark border') + ' rounded-pill">' + (labels[status] || status) + '</span>';
                        feedback.classList.remove('d-none');
                        setTimeout(function () { feedback.classList.add('d-none'); }, 2000);
                    } else {
                        alert(result.message || 'Could not update status.');
                        form.submit();
                    }
                })
                .catch(function () {
                    form.submit();
                });
        });
    }

    function initChecklist() {
        var list = document.getElementById('checklistList');
        if (!list) {
            return;
        }

        list.addEventListener('change', function (e) {
            var checkbox = e.target.closest('.checklist-toggle');
            if (!checkbox) {
                return;
            }

            var itemId = checkbox.getAttribute('data-item-id');
            var label = list.querySelector('label[for="chk-' + itemId + '"]');

            postJson('/Task/ToggleChecklistItemAjax/' + itemId, {})
                .then(function (result) {
                    if (result.success) {
                        if (label) {
                            label.classList.toggle('text-decoration-line-through');
                            label.classList.toggle('text-muted');
                            label.classList.toggle('text-dark');
                        }
                    } else {
                        checkbox.checked = !checkbox.checked;
                        alert(result.message || 'Could not update checklist item.');
                    }
                })
                .catch(function () {
                    checkbox.checked = !checkbox.checked;
                    alert('Could not update checklist item.');
                });
        });

        list.addEventListener('click', function (e) {
            var deleteBtn = e.target.closest('.checklist-delete');
            if (!deleteBtn) {
                return;
            }

            var itemId = deleteBtn.getAttribute('data-item-id');
            var row = list.querySelector('.checklist-item[data-item-id="' + itemId + '"]');

            postJson('/Task/DeleteChecklistItemAjax/' + itemId, {})
                .then(function (result) {
                    if (result.success && row) {
                        row.remove();
                    } else if (!result.success) {
                        alert(result.message || 'Could not delete checklist item.');
                    }
                })
                .catch(function () {
                    alert('Could not delete checklist item.');
                });
        });
    }

    function initAttachmentDropzone() {
        var dropzone = document.getElementById('attachmentDropzone');
        var input = document.getElementById('attachmentFileInput');
        var fileNameLabel = document.getElementById('attachmentFileName');

        if (!dropzone || !input) {
            return;
        }

        input.addEventListener('change', function () {
            fileNameLabel.textContent = input.files.length ? input.files[0].name : '';
        });

        ['dragover', 'dragenter'].forEach(function (evt) {
            dropzone.addEventListener(evt, function (e) {
                e.preventDefault();
                dropzone.classList.add('border-primary');
            });
        });

        ['dragleave', 'drop'].forEach(function (evt) {
            dropzone.addEventListener(evt, function (e) {
                e.preventDefault();
                dropzone.classList.remove('border-primary');
            });
        });

        dropzone.addEventListener('drop', function (e) {
            if (e.dataTransfer.files.length) {
                input.files = e.dataTransfer.files;
                fileNameLabel.textContent = e.dataTransfer.files[0].name;
            }
        });
    }
})();
