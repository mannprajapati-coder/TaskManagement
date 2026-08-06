(function () {
    var accessToken = document.querySelector('meta[name="access-token"]')?.getAttribute('content') || '';
    var apiBaseUrl = document.querySelector('meta[name="api-base-url"]')?.getAttribute('content') || '';
    var csrfToken = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';

    var badge = document.getElementById('notificationBadge');
    var list = document.getElementById('notificationDropdownList');
    var emptyState = document.getElementById('notificationEmptyState');

    if (!accessToken || !apiBaseUrl || !badge || !list) {
        return;
    }

    var unreadCount = 0;

    function setBadge(count) {
        unreadCount = count;
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : String(count);
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str || '';
        return div.innerHTML;
    }

    function dropdownItemHtml(n) {
        return '<li class="border-bottom">' +
            '<a href="' + escapeHtml(n.linkUrl || '#') + '" class="dropdown-item py-2 px-3 white-space-normal notification-link" data-id="' + n.id + '">' +
            '<div class="fw-semibold small text-dark">' + escapeHtml(n.title) + '</div>' +
            '<div class="extra-small text-muted">' + escapeHtml(n.message) + '</div>' +
            '</a></li>';
    }

    function prependNotification(n) {
        if (emptyState) {
            emptyState.remove();
            emptyState = null;
        }
        list.insertAdjacentHTML('afterbegin', dropdownItemHtml(n));
    }

    function markReadOnServer(id) {
        fetch('/Notifications/MarkAsRead/' + id, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken
            },
            body: JSON.stringify({})
        }).catch(function () { /* best-effort */ });
    }

    list.addEventListener('click', function (e) {
        var link = e.target.closest('.notification-link');
        if (!link) return;
        var id = link.getAttribute('data-id');
        markReadOnServer(id);
        setBadge(Math.max(0, unreadCount - 1));
    });

    function showNotificationToast(n) {
        if (typeof window.showToast !== 'function') {
            return;
        }

        var html =
            '<div class="d-flex">' +
            '<div class="toast-body">' +
            '<div class="fw-semibold"><i class="bi bi-bell-fill text-primary me-1"></i>' + escapeHtml(n.title) + '</div>' +
            '<div class="small text-secondary">' + escapeHtml(n.message) + '</div>' +
            '</div>' +
            '<button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';

        var toastEl = window.showToast(null, null, { html: html, delay: 6000 });
        if (!toastEl) {
            return;
        }

        toastEl.addEventListener('click', function (e) {
            if (e.target.closest('.btn-close')) return;
            if (n.linkUrl) {
                markReadOnServer(n.id);
                window.location.href = n.linkUrl;
            }
        });
    }

    // Seed the bell with notifications that already existed before this page load.
    fetch('/Notifications/Recent')
        .then(function (res) { return res.json(); })
        .then(function (notifications) {
            setBadge(notifications.length);
            if (notifications.length > 0 && emptyState) {
                emptyState.remove();
                emptyState = null;
            }
            notifications.forEach(function (n) {
                list.insertAdjacentHTML('beforeend', dropdownItemHtml(n));
            });
        })
        .catch(function () { /* non-fatal */ });

    if (typeof signalR === 'undefined') {
        return;
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl(apiBaseUrl + '/hubs/notifications', {
            accessTokenFactory: function () { return accessToken; }
        })
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveNotification', function (notification) {
        setBadge(unreadCount + 1);
        prependNotification(notification);
        showNotificationToast(notification);
    });

    connection.start().catch(function () {
        // Connection failures (e.g. expired token) degrade gracefully to no live updates.
    });
})();
