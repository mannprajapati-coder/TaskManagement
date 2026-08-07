(function () {
    var accessToken = document.querySelector('meta[name="access-token"]')?.getAttribute('content') || '';
    var apiBaseUrl = document.querySelector('meta[name="api-base-url"]')?.getAttribute('content') || '';
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    var widget = document.getElementById('activeTimerWidget');
    var elapsedEl = document.getElementById('activeTimerElapsed');
    var taskTitleEl = document.getElementById('activeTimerTaskTitle');
    var viewTaskLink = document.getElementById('activeTimerViewTaskLink');

    var stopModalEl = document.getElementById('stopTimerModal');
    var stopNotesEl = document.getElementById('stopTimerNotes');
    var stopConfirmBtn = document.getElementById('stopTimerConfirmBtn');
    var stopModal = (stopModalEl && typeof bootstrap !== 'undefined') ? new bootstrap.Modal(stopModalEl) : null;

    if (!accessToken) {
        return;
    }

    var activeTimer = null; // { id, taskId, taskTitle, startedAt: Date }
    var tickInterval = null;

    function postJson(url) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken
            },
            body: JSON.stringify({})
        }).then(function (res) { return res.json(); });
    }

    function pad(n) { return n < 10 ? '0' + n : String(n); }

    function formatElapsed(startedAt) {
        var totalSeconds = Math.max(0, Math.floor((Date.now() - startedAt.getTime()) / 1000));
        var hours = Math.floor(totalSeconds / 3600);
        var minutes = Math.floor((totalSeconds % 3600) / 60);
        var seconds = totalSeconds % 60;
        return pad(hours) + ':' + pad(minutes) + ':' + pad(seconds);
    }

    function tick() {
        if (!activeTimer) return;
        var text = formatElapsed(activeTimer.startedAt);
        if (elapsedEl) elapsedEl.textContent = text;
        document.querySelectorAll('[data-timer-elapsed]').forEach(function (el) {
            if (el !== elapsedEl && el.getAttribute('data-timer-task-id') === activeTimer.taskId) {
                el.textContent = text;
            }
        });
    }

    function refreshTriggerButtons() {
        document.querySelectorAll('[data-timer-task-id]').forEach(function (el) {
            var taskId = el.getAttribute('data-timer-task-id');
            var isRunning = !!activeTimer && activeTimer.taskId === taskId;
            el.classList.toggle('timer-running', isRunning);
            var icon = el.querySelector('[data-timer-icon]');
            if (icon) {
                icon.className = isRunning ? 'bi bi-stop-fill' : 'bi bi-play-fill';
            }
            var label = el.querySelector('[data-timer-label]');
            if (label) {
                label.textContent = isRunning ? 'Stop' : 'Start';
            }
            el.title = isRunning ? 'Stop timer' : 'Start timer';
        });
    }

    function applyState(timer) {
        activeTimer = timer ? {
            id: timer.id,
            taskId: timer.taskId,
            taskTitle: timer.taskTitle,
            startedAt: new Date(timer.startedAt)
        } : null;

        if (tickInterval) {
            clearInterval(tickInterval);
            tickInterval = null;
        }

        if (activeTimer) {
            if (widget) widget.classList.remove('d-none');
            if (taskTitleEl) taskTitleEl.textContent = activeTimer.taskTitle;
            if (viewTaskLink) viewTaskLink.href = '/Task/Detail/' + activeTimer.taskId;
            tick();
            tickInterval = setInterval(tick, 1000);
        } else {
            if (widget) widget.classList.add('d-none');
            if (elapsedEl) elapsedEl.textContent = '00:00:00';
        }

        refreshTriggerButtons();
        document.dispatchEvent(new CustomEvent('timetracking:changed', { detail: activeTimer }));
    }

    function seedActiveTimer() {
        return fetch('/TimeTracking/GetActiveTimer')
            .then(function (res) { return res.json(); })
            .then(function (result) {
                applyState(result && result.success ? result.data : null);
            })
            .catch(function () { /* non-fatal */ });
    }

    function startTimer(taskId) {
        postJson('/TimeTracking/StartTimer?taskId=' + encodeURIComponent(taskId))
            .then(function (result) {
                if (result.success) {
                    seedActiveTimer();
                } else {
                    if (typeof showToast === 'function') showToast(result.message || 'Could not start the timer.', 'danger');
                }
            })
            .catch(function () {
                if (typeof showToast === 'function') showToast('Could not start the timer.', 'danger');
            });
    }

    function openStopModal() {
        if (stopNotesEl) stopNotesEl.value = '';
        if (stopModal) {
            stopModal.show();
        } else {
            confirmStop();
        }
    }

    function confirmStop() {
        var notes = stopNotesEl ? stopNotesEl.value.trim() : '';
        postJson('/TimeTracking/StopTimer' + (notes ? '?notes=' + encodeURIComponent(notes) : ''))
            .then(function (result) {
                if (result.success) {
                    if (stopModal) stopModal.hide();
                    seedActiveTimer();
                } else {
                    if (typeof showToast === 'function') showToast(result.message || 'Could not stop the timer.', 'danger');
                }
            })
            .catch(function () {
                if (typeof showToast === 'function') showToast('Could not stop the timer.', 'danger');
            });
    }

    if (stopConfirmBtn) {
        stopConfirmBtn.addEventListener('click', confirmStop);
    }

    // Generic delegation: any [data-timer-task-id] element toggles that task's timer;
    // any [data-timer-stop] element always stops whatever timer is currently running.
    document.addEventListener('click', function (e) {
        var stopTrigger = e.target.closest('[data-timer-stop]');
        if (stopTrigger) {
            e.preventDefault();
            openStopModal();
            return;
        }

        var toggleTrigger = e.target.closest('[data-timer-task-id]');
        if (toggleTrigger) {
            e.preventDefault();
            var taskId = toggleTrigger.getAttribute('data-timer-task-id');
            if (activeTimer && activeTimer.taskId === taskId) {
                openStopModal();
            } else {
                startTimer(taskId);
            }
        }
    });

    seedActiveTimer();

    if (typeof signalR === 'undefined' || !apiBaseUrl) {
        return;
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl(apiBaseUrl + '/hubs/notifications', {
            accessTokenFactory: function () { return accessToken; }
        })
        .withAutomaticReconnect()
        .build();

    connection.on('TimerChanged', function () {
        seedActiveTimer();
    });

    connection.start().catch(function () {
        // Connection failures (e.g. expired token) degrade gracefully to no live updates.
    });
})();
