export class ToastService {
    showToast(message, type = 'error') {
        const container = document.getElementById('notifications');
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        const text = document.createElement('span');
        text.className = 'toast-message';
        text.textContent = message;
        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'toast-close';
        close.setAttribute('aria-label', 'Закрыть уведомление');
        close.textContent = '×';
        close.addEventListener('click', () => toast.remove());
        toast.append(text, close);
        container.appendChild(toast);
        window.setTimeout(() => toast.remove(), 10000);
    }

    showError(message) { this.showToast(message, 'error'); }
    showWarning(message) { this.showToast(message, 'warning'); }
    showSuccess(message) { this.showToast(message, 'success'); }
    showInfo(message) { this.showToast(message, 'info'); }
}
