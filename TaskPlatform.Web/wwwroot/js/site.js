// Shared toast helper used across the app for page-level success/error feedback
// (TempData messages, drag/drop failures, etc.) and reused by notifications.js for
// the richer real-time notification toasts, so there's one Bootstrap Toast
// implementation instead of several copy-pasted ones.
window.showToast = function (message, type, options) {
    options = options || {};
    var toastContainer = document.getElementById('toastContainer');
    if (!toastContainer || typeof bootstrap === 'undefined') {
        return null;
    }

    var toastEl = document.createElement('div');
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');

    if (options.html) {
        toastEl.className = 'toast align-items-center border-0 shadow-lg mb-2';
        toastEl.innerHTML = options.html;
    } else {
        var icon = type === 'danger' ? 'bi-exclamation-triangle-fill' : 'bi-check-circle-fill';
        toastEl.className = 'toast align-items-center text-white bg-' + (type === 'danger' ? 'danger' : 'success') + ' border-0 shadow-lg mb-2';
        toastEl.innerHTML = '<div class="d-flex"><div class="toast-body fw-medium"><i class="bi ' + icon + ' me-2"></i>' + message + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>';
    }

    toastContainer.appendChild(toastEl);
    var bsToast = new bootstrap.Toast(toastEl, { delay: options.delay || 6000 });
    bsToast.show();
    toastEl.addEventListener('hidden.bs.toast', function () { toastEl.remove(); });

    return toastEl;
};

// Sidebar Toggle Handler
(function () {
    var savedState = localStorage.getItem('taskPlatformSidebarCollapsed');
    if (savedState === 'true') {
        document.body.classList.add('sidebar-collapsed');
    }

    document.addEventListener('DOMContentLoaded', function () {
        var toggleBtn = document.getElementById('sidebarToggleBtn');
        var mobileToggleBtn = document.getElementById('mobileSidebarToggle');

        function toggleSidebar() {
            document.body.classList.toggle('sidebar-collapsed');
            var isCollapsed = document.body.classList.contains('sidebar-collapsed');
            localStorage.setItem('taskPlatformSidebarCollapsed', isCollapsed ? 'true' : 'false');
        }

        toggleBtn?.addEventListener('click', toggleSidebar);
        mobileToggleBtn?.addEventListener('click', toggleSidebar);
    });
})();

// Attachment Preview Modal Handler
document.addEventListener('click', function (e) {
    var btn = e.target.closest('.preview-attachment-btn');
    if (!btn) return;

    var url = btn.getAttribute('data-url');
    var title = btn.getAttribute('data-title') || 'Attachment Preview';
    var type = btn.getAttribute('data-type') || '';

    var modalEl = document.getElementById('attachmentPreviewModal');
    var titleEl = document.getElementById('previewModalTitle');
    var containerEl = document.getElementById('previewContainer');
    var downloadBtn = document.getElementById('previewModalDownloadBtn');

    if (!modalEl || !containerEl) return;

    if (titleEl) titleEl.textContent = title;
    if (downloadBtn) downloadBtn.setAttribute('href', url.replace('PreviewAttachment', 'DownloadAttachment'));

    containerEl.innerHTML = '<div class="text-center py-5 text-muted"><div class="spinner-border text-primary me-2"></div>Loading preview...</div>';

    var modal = new bootstrap.Modal(modalEl);
    modal.show();

    if (type.startsWith('image/')) {
        var img = document.createElement('img');
        img.src = url;
        img.alt = title;
        img.className = 'img-fluid rounded shadow';
        img.onload = function () {
            containerEl.innerHTML = '';
            containerEl.appendChild(img);
        };
        img.onerror = function () {
            containerEl.innerHTML = '<div class="text-center py-5 text-muted"><i class="bi bi-exclamation-triangle text-danger display-4 d-block mb-2"></i>Unable to preview image.</div>';
        };
    } else {
        var iframe = document.createElement('iframe');
        iframe.src = url;
        iframe.onload = function () {
            // Success
        };
        containerEl.innerHTML = '';
        containerEl.appendChild(iframe);
    }
});
