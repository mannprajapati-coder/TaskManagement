(function () {
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    var badgeClasses = {
        Completed: 'bg-success-subtle text-success border border-success',
        InProgress: 'bg-primary-subtle text-primary border border-primary',
        InReview: 'bg-warning-subtle text-dark border border-warning',
        Cancelled: 'bg-secondary-subtle text-secondary border border-secondary',
        Todo: 'bg-light text-dark border'
    };
    var statusLabels = { InProgress: 'In Progress', InReview: 'In Review' };
    var loadedSubtaskPanels = {};

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

    // Event delegation on document: handlers are bound once and also cover
    // subtask panels injected later via fetch (see initSubtaskPanels below).
    initStatusForms();
    initChecklist();
    initSubtaskPanels();
    initAttachmentDropzone();

    function initStatusForms() {
        document.addEventListener('submit', function (e) {
            var form = e.target.closest('.task-status-form');
            if (!form) {
                return;
            }
            e.preventDefault();

            var taskId = form.querySelector('input[name="TaskId"]').value;
            var select = form.querySelector('select[name="Status"]');
            var subtaskId = form.getAttribute('data-subtask-id');

            postJson('/Task/UpdateStatusAjax', { TaskId: taskId, Status: select.value })
                .then(function (result) {
                    if (!result.success) {
                        alert(result.message || 'Could not update status.');
                        return;
                    }

                    var status = result.status || select.value;

                    if (subtaskId) {
                        updateSubtaskBadge(subtaskId, status);
                        reloadSubtaskPanel(subtaskId);
                    } else {
                        var statusDisplay = document.getElementById('statusDisplay');
                        var feedback = document.getElementById('statusUpdateFeedback');
                        if (statusDisplay) {
                            statusDisplay.innerHTML = '<span class="badge ' + (badgeClasses[status] || 'bg-light text-dark border') + ' rounded-pill">' + (statusLabels[status] || status) + '</span>';
                        }
                        if (feedback) {
                            feedback.classList.remove('d-none');
                            setTimeout(function () { feedback.classList.add('d-none'); }, 2000);
                        }
                    }
                })
                .catch(function () {
                    alert('Could not update status.');
                });
        });
    }

    function updateSubtaskBadge(subtaskId, status) {
        var toggle = document.querySelector('.subtask-toggle[data-subtask-id="' + subtaskId + '"]');
        if (!toggle) {
            return;
        }
        var badge = toggle.querySelector('.badge');
        var title = toggle.querySelector('span:not(.badge)');
        if (badge) {
            badge.className = 'badge ' + (badgeClasses[status] || 'bg-light text-dark border') + ' rounded-pill';
            badge.textContent = statusLabels[status] || status;
        }
        if (title) {
            title.classList.toggle('text-decoration-line-through', status === 'Completed');
            title.classList.toggle('text-muted', status === 'Completed');
            title.classList.toggle('text-dark', status !== 'Completed');
        }
    }

    function initChecklist() {
        document.addEventListener('change', function (e) {
            var checkbox = e.target.closest('.checklist-toggle');
            if (!checkbox) {
                return;
            }

            var itemId = checkbox.getAttribute('data-item-id');
            var label = document.querySelector('label[for="chk-' + itemId + '"]');

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

        document.addEventListener('click', function (e) {
            var deleteBtn = e.target.closest('.checklist-delete');
            if (!deleteBtn) {
                return;
            }

            var itemId = deleteBtn.getAttribute('data-item-id');
            var row = document.querySelector('.checklist-item[data-item-id="' + itemId + '"]');

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

    function initSubtaskPanels() {
        document.addEventListener('shown.bs.collapse', function (e) {
            var container = e.target.querySelector('.subtask-panel-container');
            if (!container) {
                return;
            }
            var subtaskId = container.getAttribute('data-subtask-id');
            if (loadedSubtaskPanels[subtaskId]) {
                return;
            }
            loadSubtaskPanel(subtaskId, container);
        });
    }

    function loadSubtaskPanel(subtaskId, container) {
        var toggle = document.querySelector('.subtask-toggle[data-subtask-id="' + subtaskId + '"]');
        var projectId = toggle ? toggle.getAttribute('data-project-id') : '';

        fetch('/Task/GetSubtaskPanel/' + subtaskId + '?projectId=' + projectId)
            .then(function (res) {
                if (!res.ok) {
                    throw new Error('Failed to load subtask.');
                }
                return res.text();
            })
            .then(function (html) {
                container.innerHTML = html;
                loadedSubtaskPanels[subtaskId] = true;
            })
            .catch(function () {
                container.innerHTML = '<p class="text-danger small mb-0">Could not load subtask details.</p>';
            });
    }

    function reloadSubtaskPanel(subtaskId) {
        var container = document.querySelector('.subtask-panel-container[data-subtask-id="' + subtaskId + '"]');
        if (container) {
            loadSubtaskPanel(subtaskId, container);
        }
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
