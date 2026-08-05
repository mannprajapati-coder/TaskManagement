(function () {
    var calendarEl = document.getElementById('calendar');
    if (!calendarEl || typeof FullCalendar === 'undefined') {
        return;
    }

    var workspaceId = calendarEl.getAttribute('data-workspace-id');
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    if (!workspaceId) {
        return;
    }

    function parseDateOnly(s) {
        var parts = s.split('-').map(Number);
        return new Date(Date.UTC(parts[0], parts[1] - 1, parts[2]));
    }

    function addDays(date, days) {
        var d = new Date(date.getTime());
        d.setUTCDate(d.getUTCDate() + days);
        return d;
    }

    function formatDateOnly(date) {
        return date.toISOString().slice(0, 10);
    }

    // Our API's End is an inclusive due date; FullCalendar's all-day `end` is exclusive.
    function toFullCalendarEnd(inclusiveEndStr) {
        return formatDateOnly(addDays(parseDateOnly(inclusiveEndStr), 1));
    }

    function toInclusiveDueDate(exclusiveEndStr) {
        return formatDateOnly(addDays(parseDateOnly(exclusiveEndStr), -1));
    }

    function reschedule(taskId, newStartDate, newDueDate, revertFn) {
        fetch('/Calendar/Reschedule', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken
            },
            body: JSON.stringify({
                TaskId: taskId,
                NewStartDate: newStartDate,
                NewDueDate: newDueDate
            })
        }).then(function (res) {
            if (!res.ok) {
                throw new Error('Request failed with status ' + res.status);
            }
            return res.json();
        }).then(function (result) {
            if (!result.success) {
                alert(result.message || 'Could not reschedule task.');
                revertFn();
            }
        }).catch(function () {
            alert('Could not reschedule task.');
            revertFn();
        });
    }

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        editable: true,
        eventResizableFromStart: true,
        height: 'auto',

        events: function (fetchInfo, successCallback, failureCallback) {
            var url = '/Calendar/Events?workspaceId=' + encodeURIComponent(workspaceId) +
                '&start=' + fetchInfo.startStr.slice(0, 10) +
                '&end=' + fetchInfo.endStr.slice(0, 10);

            fetch(url)
                .then(function (res) { return res.json(); })
                .then(function (events) {
                    successCallback(events.map(function (e) {
                        return {
                            id: e.id,
                            title: e.title,
                            start: e.start,
                            end: e.end ? toFullCalendarEnd(e.end) : undefined,
                            url: e.url,
                            allDay: e.allDay,
                            color: e.color
                        };
                    }));
                })
                .catch(failureCallback);
        },

        eventClick: function (info) {
            if (info.event.url) {
                info.jsEvent.preventDefault();
                window.location.href = info.event.url;
            }
        },

        eventDrop: function (info) {
            var newStart = info.event.startStr;
            var newEnd = info.event.endStr ? toInclusiveDueDate(info.event.endStr) : newStart;
            reschedule(info.event.id, newStart, newEnd, function () { info.revert(); });
        },

        eventResize: function (info) {
            var newStart = info.event.startStr;
            var newEnd = info.event.endStr ? toInclusiveDueDate(info.event.endStr) : newStart;
            reschedule(info.event.id, newStart, newEnd, function () { info.revert(); });
        }
    });

    calendar.render();
})();
