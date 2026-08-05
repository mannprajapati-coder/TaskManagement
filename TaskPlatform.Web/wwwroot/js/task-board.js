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
            if (!res.ok) {
                throw new Error('Request failed with status ' + res.status);
            }
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
                    window.location.reload();
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

    board.addEventListener('click', function (e) {
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
                    alert(result.message || 'Could not move task.');
                }
            })
            .catch(function () {
                alert('Could not move task. Please try again.');
            });
    });
})();
