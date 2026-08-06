(function () {
    var board = document.getElementById('kanbanBoard');
    if (!board) {
        return;
    }

    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';
    var draggedCard = null;

    function postJson(url, payload) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken
            },
            body: JSON.stringify(payload)
        }).then(function (res) {
            return res.json();
        });
    }

    function updateColumnCounts() {
        board.querySelectorAll('.kanban-column').forEach(function (column) {
            var body = column.querySelector('.kanban-column-body');
            var count = body.querySelectorAll('.task-card').length;
            var badge = column.querySelector('.column-count');
            if (badge) {
                badge.textContent = count;
            }
        });
    }

    function orderedTaskIdsInColumn(columnBody) {
        return Array.from(columnBody.querySelectorAll('.task-card')).map(function (card) {
            return card.getAttribute('data-task-id');
        });
    }

    board.addEventListener('dragstart', function (e) {
        var card = e.target.closest('.task-card');
        if (!card) {
            return;
        }
        draggedCard = card;
        card.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', card.getAttribute('data-task-id'));
    });

    board.addEventListener('dragend', function (e) {
        var card = e.target.closest('.task-card');
        if (card) {
            card.classList.remove('dragging');
        }
        board.querySelectorAll('.kanban-column-body.drag-over').forEach(function (el) {
            el.classList.remove('drag-over');
        });
    });

    board.querySelectorAll('.kanban-column-body').forEach(function (columnBody) {
        columnBody.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            columnBody.classList.add('drag-over');

            if (!draggedCard) {
                return;
            }

            var afterElement = getDragAfterElement(columnBody, e.clientY);
            if (afterElement == null) {
                columnBody.appendChild(draggedCard);
            } else {
                columnBody.insertBefore(draggedCard, afterElement);
            }
        });

        columnBody.addEventListener('dragleave', function (e) {
            if (e.target === columnBody) {
                columnBody.classList.remove('drag-over');
            }
        });

        columnBody.addEventListener('drop', function (e) {
            e.preventDefault();
            columnBody.classList.remove('drag-over');

            if (!draggedCard) {
                return;
            }

            var taskId = draggedCard.getAttribute('data-task-id');
            var newStatus = columnBody.getAttribute('data-status');
            draggedCard.setAttribute('data-status', newStatus);

            updateColumnCounts();

            postJson('/Task/Reorder', {
                TaskId: taskId,
                Status: newStatus,
                OrderedTaskIds: orderedTaskIdsInColumn(columnBody)
            }).then(function (result) {
                if (!result.success) {
                    showToast(result.message || 'Please complete all subtasks before marking the parent task as completed.', 'danger');
                    setTimeout(function () { window.location.reload(); }, 1200);
                }
            }).catch(function () {
                window.location.reload();
            });
        });
    });

    function getDragAfterElement(columnBody, y) {
        var cards = Array.from(columnBody.querySelectorAll('.task-card:not(.dragging)'));
        return cards.reduce(function (closest, card) {
            var box = card.getBoundingClientRect();
            var offset = y - box.top - box.height / 2;
            if (offset < 0 && offset > closest.offset) {
                return { offset: offset, element: card };
            }
            return closest;
        }, { offset: Number.NEGATIVE_INFINITY, element: null }).element;
    }

    // Selected-card state: click anywhere on a card (outside its interactive
    // controls) to highlight it, and remember the choice across navigation so
    // returning from a task's Detail page shows where you left off.
    var SELECTED_TASK_KEY = 'taskPlatformSelectedTaskId';

    function selectCard(card) {
        if (!card) {
            return;
        }
        board.querySelectorAll('.task-card.selected').forEach(function (el) {
            el.classList.remove('selected');
        });
        card.classList.add('selected');
        sessionStorage.setItem(SELECTED_TASK_KEY, card.getAttribute('data-task-id'));
    }

    (function restoreSelection() {
        var savedId = sessionStorage.getItem(SELECTED_TASK_KEY);
        if (!savedId) {
            return;
        }
        var card = board.querySelector('.task-card[data-task-id="' + savedId + '"]');
        if (card) {
            card.classList.add('selected');
        }
    })();

    board.addEventListener('click', function (e) {
        var card = e.target.closest('.task-card');
        if (card) {
            if (!e.target.closest('a, button, .dropdown')) {
                selectCard(card);
            } else if (e.target.closest('a')) {
                // Navigating into the task's Detail page — remember it was the last one viewed.
                sessionStorage.setItem(SELECTED_TASK_KEY, card.getAttribute('data-task-id'));
            }
        }

        var moveBtn = e.target.closest('.move-task-btn');
        if (!moveBtn) {
            return;
        }

        var taskId = moveBtn.getAttribute('data-task-id');
        var targetStatus = moveBtn.getAttribute('data-target-status');

        postJson('/Task/UpdateStatusAjax', { TaskId: taskId, Status: targetStatus })
            .then(function (result) {
                if (result.success) {
                    window.location.reload();
                } else {
                    showToast(result.message || 'Please complete all subtasks before marking the parent task as completed.', 'danger');
                }
            })
            .catch(function () {
                showToast('Could not move task. Please complete all subtasks before marking the parent task as completed.', 'danger');
            });
    });
})();
