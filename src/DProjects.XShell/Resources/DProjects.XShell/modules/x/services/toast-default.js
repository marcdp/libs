export default class Toast {

    // fields
    _container = null;
    _items = new Map();
    _nextId = 1;

    // methods
    show(message, type = "info", duration = 4000) {
        const id = String(this._nextId++);

        const element = document.createElement("div");
        element.className = `x-toast x-toast-${type}`;
        element.textContent = message;
        element.dataset.toastId = id;

        this._getContainer().append(element);
        this._items.set(id, {
            element,
            timer: duration > 0
                ? setTimeout(() => this.close(id), duration)
                : null
        });

        this.dispatchEvent(new CustomEvent("shown", {
            detail: { id }
        }));

        return id;
    }

    close(id) {
        const item = this._items.get(id);
        if (!item) return;

        if (item.timer) {
            clearTimeout(item.timer);
        }

        item.element.remove();
        this._items.delete(id);

        this.dispatchEvent(new CustomEvent("closed", {
            detail: { id }
        }));
    }

    clear() {
        for (const id of [...this._items.keys()]) {
            this.close(id);
        }
    }

    _getContainer() {
        if (this._container) return this._container;

        const container = document.createElement("div");
        container.className = "x-toast-container";
        container.setAttribute("aria-live", "polite");
        container.setAttribute("aria-atomic", "false");

        document.body.append(container);
        this._container = container;

        return container;
    }
}