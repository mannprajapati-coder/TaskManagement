(function () {
    var accessToken = document.querySelector('meta[name="access-token"]')?.getAttribute('content') || '';
    var apiBaseUrl = document.querySelector('meta[name="api-base-url"]')?.getAttribute('content') || '';
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    var activeTimerDot = document.getElementById('activeTimerDot');
    var activeTimerSection = document.getElementById('activeTimerSection');
    var elapsedEl = document.getElementById('activeTimerElapsed');
    var taskTitleEl = document.getElementById('activeTimerTaskTitle');
    var viewTaskLink = document.getElementById('activeTimerViewTaskLink');
    var statusBadgeEl = document.getElementById('activeTimerStatusBadge');
    var pauseResumeBtn = document.getElementById('activeTimerPauseResumeBtn');
    var timersBtn = document.getElementById('timersBtn');
    var timerTaskList = document.getElementById('timerTaskList');

    var stopModalEl = document.getElementById('stopTimerModal');
    var stopNotesEl = document.getElementById('stopTimerNotes');
    var stopConfirmBtn = document.getElementById('stopTimerConfirmBtn');
    var stopModal = (stopModalEl && typeof bootstrap !== 'undefined') ? new bootstrap.Modal(stopModalEl) : null;

    if (!accessToken) {
        return;
    }

    var activeTimer = null; // { id, taskId, taskTitle, status: 'Running'|'Paused', baseElapsedSeconds, syncedAt: ms }
    var tickInterval = null;
    var taskOptions = null; // cached list of { id, title, status } for the current workspace
    var taskListLoaded = false;

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

    function formatElapsed(totalSeconds) {
        totalSeconds = Math.max(0, Math.floor(totalSeconds));
        var hours = Math.floor(totalSeconds / 3600);
        var minutes = Math.floor((totalSeconds % 3600) / 60);
        var seconds = totalSeconds % 60;
        return pad(hours) + ':' + pad(minutes) + ':' + pad(seconds);
    }

    function currentElapsedSeconds() {
        if (!activeTimer) return 0;
        var extra = activeTimer.status === 'Running' ? (Date.now() - activeTimer.syncedAt) / 1000 : 0;
        return activeTimer.baseElapsedSeconds + extra;
    }

    function tick() {
        if (!activeTimer || !elapsedEl) return;
        elapsedEl.textContent = formatElapsed(currentElapsedSeconds());
    }

    function updateStatusUi() {
        if (!activeTimer) return;
        var isRunning = activeTimer.status === 'Running';

        if (statusBadgeEl) {
            statusBadgeEl.textContent = isRunning ? 'Running' : 'Paused';
            statusBadgeEl.className = 'badge rounded-pill ' + (isRunning ? 'bg-success-subtle text-success' : 'bg-warning-subtle text-warning');
        }

        if (pauseResumeBtn) {
            pauseResumeBtn.innerHTML = isRunning
                ? '<i class="bi bi-pause-fill me-1"></i>Pause'
                : '<i class="bi bi-play-fill me-1"></i>Resume';
        }

        if (timersBtn) {
            timersBtn.classList.toggle('timer-pulse', isRunning);
        }
    }

    function refreshTriggerButtons() {
        document.querySelectorAll('[data-timer-task-id]').forEach(function (el) {
            var taskId = el.getAttribute('data-timer-task-id');
            var isTracking = !!activeTimer && activeTimer.taskId === taskId;
            el.classList.toggle('timer-running', isTracking);
            var icon = el.querySelector('[data-timer-icon]');
            if (icon) {
                icon.className = isTracking ? 'bi bi-stop-fill' : 'bi bi-play-fill';
            }
            var label = el.querySelector('[data-timer-label]');
            if (label) {
                label.textContent = isTracking ? 'Stop' : 'Start';
            }
            el.title = isTracking ? 'Stop timer' : 'Start timer';
        });
    }

    function renderTaskList() {
        if (!timerTaskList) return;

        if (taskOptions === null) {
            timerTaskList.innerHTML = '<div class="text-center text-muted small py-3">Loading tasks…</div>';
            return;
        }

        if (!taskOptions.length) {
            timerTaskList.innerHTML = '<div class="text-center text-muted small py-3">No tasks in this workspace yet.</div>';
            return;
        }

        timerTaskList.innerHTML = '';
        taskOptions.forEach(function (task) {
            var isTracking = !!activeTimer && activeTimer.taskId === task.id;

            var row = document.createElement('div');
            row.className = 'd-flex align-items-center justify-content-between gap-2 py-2 border-bottom';

            var titleWrap = document.createElement('div');
            titleWrap.className = 'flex-grow-1 text-truncate small';
            titleWrap.title = task.title;
            titleWrap.textContent = task.title;
            row.appendChild(titleWrap);

            if (isTracking) {
                var badge = document.createElement('span');
                badge.className = 'badge bg-primary-subtle text-primary rounded-pill';
                badge.textContent = 'Tracking';
                row.appendChild(badge);
            } else {
                var startBtn = document.createElement('button');
                startBtn.type = 'button';
                startBtn.className = 'btn btn-sm btn-outline-custom rounded-circle';
                startBtn.title = 'Start timer';
                startBtn.innerHTML = '<i class="bi bi-play-fill"></i>';
                startBtn.addEventListener('click', function () {
                    startTimer(task.id);
                });
                row.appendChild(startBtn);
            }

            timerTaskList.appendChild(row);
        });
    }

    function loadTaskOptions() {
        return fetch('/TimeTracking/GetMyTaskOptions')
            .then(function (res) { return res.json(); })
            .then(function (result) {
                taskOptions = Array.isArray(result) ? result : [];
                taskListLoaded = true;
                renderTaskList();
            })
            .catch(function () {
                taskOptions = [];
                renderTaskList();
            });
    }

    function applyState(timer) {
        activeTimer = timer ? {
            id: timer.id,
            taskId: timer.taskId,
            taskTitle: timer.taskTitle,
            status: timer.status || 'Running',
            baseElapsedSeconds: timer.elapsedSeconds || 0,
            syncedAt: Date.now()
        } : null;

        if (tickInterval) {
            clearInterval(tickInterval);
            tickInterval = null;
        }

        if (activeTimer) {
            if (activeTimerDot) activeTimerDot.classList.remove('d-none');
            if (activeTimerSection) activeTimerSection.classList.remove('d-none');
            if (taskTitleEl) taskTitleEl.textContent = activeTimer.taskTitle;
            if (viewTaskLink) viewTaskLink.href = '/Task/Detail/' + activeTimer.taskId;
            updateStatusUi();
            tick();
            if (activeTimer.status === 'Running') {
                tickInterval = setInterval(tick, 1000);
            }
        } else {
            if (activeTimerDot) activeTimerDot.classList.add('d-none');
            if (activeTimerSection) activeTimerSection.classList.add('d-none');
            if (elapsedEl) elapsedEl.textContent = '00:00:00';
            if (timersBtn) timersBtn.classList.remove('timer-pulse');
        }

        refreshTriggerButtons();
        renderTaskList();
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

    function pauseTimer() {
        postJson('/TimeTracking/PauseTimer')
            .then(function (result) {
                if (result.success) {
                    seedActiveTimer();
                } else {
                    if (typeof showToast === 'function') showToast(result.message || 'Could not pause the timer.', 'danger');
                }
            })
            .catch(function () {
                if (typeof showToast === 'function') showToast('Could not pause the timer.', 'danger');
            });
    }

    function resumeTimer() {
        postJson('/TimeTracking/ResumeTimer')
            .then(function (result) {
                if (result.success) {
                    seedActiveTimer();
                } else {
                    if (typeof showToast === 'function') showToast(result.message || 'Could not resume the timer.', 'danger');
                }
            })
            .catch(function () {
                if (typeof showToast === 'function') showToast('Could not resume the timer.', 'danger');
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

    if (pauseResumeBtn) {
        pauseResumeBtn.addEventListener('click', function () {
            if (!activeTimer) return;
            if (activeTimer.status === 'Running') {
                pauseTimer();
            } else {
                resumeTimer();
            }
        });
    }

    if (timersBtn) {
        timersBtn.addEventListener('click', function () {
            if (!taskListLoaded) {
                loadTaskOptions();
            }
        });
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
        taskListLoaded = false;
    });

    connection.start().catch(function () {
        // Connection failures (e.g. expired token) degrade gracefully to no live updates.
    });
})();
