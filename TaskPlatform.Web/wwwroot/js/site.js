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
