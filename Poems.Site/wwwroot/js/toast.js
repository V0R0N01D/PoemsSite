export class ToastService {
    showToast(message, type = "error") {
        Toastify({
            text: message,
            duration: 10000,
            gravity: "top",
            position: "right",
            className: `toast-${type}`,
            close: true,
            style: {
                background: this.getBackgroundColor(type),
                color: "#ffffff",
                "box-shadow": "0 2px 8px rgba(0,0,0,0.2)",
                "border-radius": "4px"
            }
        }).showToast();
    }

    getBackgroundColor(type) {
        const colors = {
            error: "#dc3545",
            warning: "#ffc107",
            success: "#28a745",
            info: "#17a2b8"
        };
        return colors[type] || colors.error;
    }


    showError(message) {
        this.showToast(message, "error");
    }

    showWarning(message) {
        this.showToast(message, "warning");
    }

    showSuccess(message) {
        this.showToast(message, "success");
    }

    showInfo(message) {
        this.showToast(message, "info");
    }
}