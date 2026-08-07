(function () {
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    var badgeClasses = {
        Completed: 'badge-completed',
        InProgress: 'badge-inprogress',
        InReview: 'badge-review',
        Cancelled: 'badge-todo',
        Todo: 'badge-todo'
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

    // Shows a dismissible inline message right after `anchor` instead of a blocking
    // browser alert() — used for permission-denied and validation errors so the user
    // sees the reason next to the control they just used, and can keep working.
    function showInlineFeedback(anchor, message, isError) {
        if (!anchor || !anchor.parentElement) {
            return;
        }

        var existing = anchor.parentElement.querySelector(':scope > .inline-feedback');
        if (existing) {
            existing.remove();
        }

        var el = document.createElement('div');
        el.className = 'inline-feedback alert ' + (isError ? 'alert-danger' : 'alert-success') + ' py-2 px-3 mt-2 mb-0 small d-flex justify-content-between align-items-center';

        var text = document.createElement('span');
        text.textContent = message;
        el.appendChild(text);

        var closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'btn-close ms-2';
        closeBtn.style.fontSize = '0.65rem';
        closeBtn.setAttribute('aria-label', 'Dismiss');
        closeBtn.addEventListener('click', function () { el.remove(); });
        el.appendChild(closeBtn);

        anchor.insertAdjacentElement('afterend', el);

        setTimeout(function () {
            if (el.isConnected) {
                el.remove();
            }
        }, isError ? 6000 : 3000);
    }

    initStatusForms();
    initChecklist();
    initSubtaskPanels();
    initSubtaskQuickActions();
    initAttachmentDropzone();
    initAttachmentMentions();

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
                        showInlineFeedback(form, result.message || 'Could not update status.', true);
                        return;
                    }

                    var status = result.status || select.value;

                    if (subtaskId) {
                        updateSubtaskBadge(subtaskId, status);
                        reloadSubtaskPanel(subtaskId);
                    } else {
                        var statusDisplay = document.getElementById('statusDisplay');
                        if (statusDisplay) {
                            statusDisplay.innerHTML = '<span class="badge-status ' + (badgeClasses[status] || 'badge-todo') + '">' + (statusLabels[status] || status) + '</span>';
                        }
                        showInlineFeedback(form, 'Status updated.', false);
                    }
                })
                .catch(function () {
                    showInlineFeedback(form, 'Could not update status. Please try again.', true);
                });
        });
    }

    function updateSubtaskBadge(subtaskId, status) {
        var toggle = document.querySelector('.subtask-toggle[data-subtask-id="' + subtaskId + '"]');
        if (!toggle) {
            return;
        }
        var badge = toggle.querySelector('.badge-status');
        var title = toggle.querySelector('span:not(.badge-status)');
        if (badge) {
            badge.className = 'badge-status ' + (badgeClasses[status] || 'badge-todo');
            badge.textContent = statusLabels[status] || status;
        }
        if (title) {
            title.classList.toggle('text-decoration-line-through', status === 'Completed');
            title.classList.toggle('text-muted', status === 'Completed');
            title.classList.toggle('text-dark', status !== 'Completed');
        }
    }

    function updateChecklistProgress() {
        var allCheckboxes = document.querySelectorAll('.checklist-toggle');
        var checkedBoxes = document.querySelectorAll('.checklist-toggle:checked');
        var total = allCheckboxes.length;
        var checked = checkedBoxes.length;
        var pct = total > 0 ? Math.round((checked / total) * 100) : 0;

        var progressBar = document.querySelector('.progress-bar-gradient');
        if (progressBar) {
            progressBar.style.width = pct + '%';
            progressBar.setAttribute('aria-valuenow', pct);
        }

        var pctText = document.getElementById('checklistPctText');
        if (pctText) {
            pctText.textContent = pct + '% Done';
        }

        var countBadge = document.getElementById('checklistCountBadge');
        if (countBadge) {
            countBadge.textContent = checked + '/' + total;
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
            var row = document.querySelector('.checklist-item[data-item-id="' + itemId + '"]');

            postJson('/Task/ToggleChecklistItemAjax/' + itemId, {})
                .then(function (result) {
                    if (result.success) {
                        if (label) {
                            label.classList.toggle('text-decoration-line-through', checkbox.checked);
                            label.classList.toggle('text-muted', checkbox.checked);
                            label.classList.toggle('text-dark', !checkbox.checked);
                        }
                        updateChecklistProgress();
                    } else {
                        checkbox.checked = !checkbox.checked;
                        showInlineFeedback(row, result.message || 'Could not update checklist item.', true);
                    }
                })
                .catch(function () {
                    checkbox.checked = !checkbox.checked;
                    showInlineFeedback(row, 'Could not update checklist item. Please try again.', true);
                });
        });

        document.addEventListener('click', function (e) {
            var deleteBtn = e.target.closest('.checklist-delete');
            if (!deleteBtn) {
                return;
            }

            var itemId = deleteBtn.getAttribute('data-item-id');
            var deleteRow = document.querySelector('.checklist-item[data-item-id="' + itemId + '"]');

            postJson('/Task/DeleteChecklistItemAjax/' + itemId, {})
                .then(function (result) {
                    if (result.success && deleteRow) {
                        deleteRow.remove();
                        updateChecklistProgress();
                    } else if (!result.success) {
                        showInlineFeedback(deleteRow, result.message || 'Could not delete checklist item.', true);
                    }
                })
                .catch(function () {
                    showInlineFeedback(deleteRow, 'Could not delete checklist item. Please try again.', true);
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

    function initSubtaskQuickActions() {
        document.addEventListener('click', function (e) {
            var completeBtn = e.target.closest('.subtask-quick-complete');
            if (completeBtn) {
                var subtaskId = completeBtn.getAttribute('data-subtask-id');
                var row = completeBtn.closest('.subtask-item');

                postJson('/Task/UpdateStatusAjax', { TaskId: subtaskId, Status: 'Completed' })
                    .then(function (result) {
                        if (result.success) {
                            updateSubtaskBadge(subtaskId, result.status || 'Completed');
                            reloadSubtaskPanel(subtaskId);
                            completeBtn.remove();
                        } else {
                            showInlineFeedback(row, result.message || 'Could not complete subtask.', true);
                        }
                    })
                    .catch(function () {
                        showInlineFeedback(row, 'Could not complete subtask. Please try again.', true);
                    });
                return;
            }

            var deleteBtn = e.target.closest('.subtask-quick-delete');
            if (deleteBtn) {
                var deleteSubtaskId = deleteBtn.getAttribute('data-subtask-id');
                var deleteRow = deleteBtn.closest('.subtask-item');

                if (!window.confirm('Delete this subtask?')) {
                    return;
                }

                postJson('/Task/DeleteSubtaskAjax/' + deleteSubtaskId, {})
                    .then(function (result) {
                        if (result.success && deleteRow) {
                            deleteRow.remove();
                        } else if (!result.success) {
                            showInlineFeedback(deleteRow, result.message || 'Could not delete subtask.', true);
                        }
                    })
                    .catch(function () {
                        showInlineFeedback(deleteRow, 'Could not delete subtask. Please try again.', true);
                    });
            }
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

    // Lets a user type "#" in the comment box to pick one of the task's attachments,
    // inserting a "#[FileName]" token and a hidden MentionedAttachmentIds input so the
    // mention round-trips through the existing AddComment form post.
    function initAttachmentMentions() {
        var textarea = document.getElementById('commentTextArea');
        var menu = document.getElementById('attachmentMentionMenu');
        var dataEl = document.getElementById('taskAttachmentsData');
        var hiddenContainer = document.getElementById('mentionedAttachmentInputs');

        if (!textarea || !menu || !dataEl || !hiddenContainer) {
            return;
        }

        var attachments = [];
        try {
            attachments = JSON.parse(dataEl.textContent || '[]');
        } catch (e) {
            attachments = [];
        }

        var mentionStart = -1;

        function closeMenu() {
            menu.classList.add('d-none');
            menu.innerHTML = '';
            mentionStart = -1;
        }

        function selectAttachment(att) {
            var value = textarea.value;
            var cursor = textarea.selectionStart;
            var before = value.slice(0, mentionStart);
            var after = value.slice(cursor);
            var inserted = '#[' + att.fileName + '] ';

            textarea.value = before + inserted + after;
            var newCursor = (before + inserted).length;
            textarea.focus();
            textarea.setSelectionRange(newCursor, newCursor);

            if (!hiddenContainer.querySelector('input[value="' + att.id + '"]')) {
                var hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = 'MentionedAttachmentIds';
                hidden.value = att.id;
                hiddenContainer.appendChild(hidden);
            }

            closeMenu();
        }

        function openMenuWithMatches(matches) {
            menu.innerHTML = '';
            if (!matches.length) {
                closeMenu();
                return;
            }
            matches.forEach(function (att) {
                var item = document.createElement('button');
                item.type = 'button';
                item.className = 'dropdown-item rounded-2 small py-2 d-flex align-items-center';
                item.innerHTML = '<i class="bi bi-paperclip me-2 text-primary"></i>' + att.fileName;
                item.addEventListener('click', function () {
                    selectAttachment(att);
                });
                menu.appendChild(item);
            });
            menu.classList.remove('d-none');
        }

        textarea.addEventListener('input', function () {
            var cursor = textarea.selectionStart;
            var value = textarea.value;
            var hashIndex = value.lastIndexOf('#', cursor - 1);

            if (hashIndex === -1) {
                closeMenu();
                return;
            }

            var between = value.slice(hashIndex + 1, cursor);
            if (/\s/.test(between)) {
                closeMenu();
                return;
            }

            mentionStart = hashIndex;
            var query = between.toLowerCase();
            var matches = attachments.filter(function (a) {
                return a.fileName.toLowerCase().indexOf(query) !== -1;
            }).slice(0, 8);

            openMenuWithMatches(matches);
        });

        textarea.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeMenu();
            }
        });

        document.addEventListener('click', function (e) {
            if (e.target !== textarea && !menu.contains(e.target)) {
                closeMenu();
            }
        });
    }
})();
