export default class Toast extends EventTarget {

    // fields
    _containers = new Map();
    _items = new Map();
    _nextId = 1;

    // ctor
    constructor({ loader }) {
        super();

        // x-notice owns the visual representation of a toast.
        // Creating an undefined custom element is valid; it will upgrade
        // when the component loader finishes registering it.
        this._noticeReady = loader.load("component:x-notice");
    }

    // methods
    show(message, {
        type = "info",
        duration = 4000,
        page = null
    } = {}) {
        const id = String(this._nextId++);
        const owner = page ?? null;
        const container = this._getContainer(owner);

        const element = document.createElement("x-notice");
        element.className = "x-toast";
        element.dataset.toastId = id;
        element.setAttribute("type", type);
        element.setAttribute("message", message);

        container.element.append(element);

        const item = {
            id,
            owner,
            element,
            timer: null
        };

        if (duration > 0) {
            item.timer = setTimeout(() => this.close(id), duration);
        }

        this._items.set(id, item);

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

        this._removeContainerIfEmpty(item.owner);

        this.dispatchEvent(new CustomEvent("closed", {
            detail: { id }
        }));
    }

    clear(page = undefined) {
        for (const [id, item] of [...this._items]) {
            if (page === undefined || item.owner === page) {
                this.close(id);
            }
        }
    }

    // methods (private)
    _getContainer(owner) {
        const existing = this._containers.get(owner);
        if (existing) return existing;

        const element = document.createElement("div");
        element.className = owner
            ? "x-toast-container x-toast-container-page"
            : "x-toast-container x-toast-container-app";

        element.setAttribute("aria-live", "polite");
        element.setAttribute("aria-atomic", "false");
        element.setAttribute("aria-relevant", "additions");

        const container = {
            element,
            host: null,
            onLoad: null,
            onClose: null
        };

        if (owner) {
            const host = owner.host;

            if (!host) {
                throw new Error("Cannot show a page toast before the Page is mounted.");
            }

            container.host = host;

            // The container remains in the Page's light DOM and is therefore
            // projected through whichever layout the Page is currently using.
            host.append(element);

            // An x-page host can later be reused for another Page. Toasts
            // belonging to the previous Page must not follow it.
            container.onLoad = event => {
                if (event.detail?.page !== owner) {
                    this.clear(owner);
                }
            };

            // Dialog/stack Pages normally close through the x-page host.
            container.onClose = () => {
                this.clear(owner);
            };

            host.addEventListener("load", container.onLoad);
            host.addEventListener("close", container.onClose);
        } else {
            document.body.append(element);
        }

        this._containers.set(owner, container);

        return container;
    }

    _removeContainerIfEmpty(owner) {
        for (const item of this._items.values()) {
            if (item.owner === owner) return;
        }

        const container = this._containers.get(owner);
        if (!container) return;

        if (container.host) {
            if (container.onLoad) {
                container.host.removeEventListener("load", container.onLoad);
            }

            if (container.onClose) {
                container.host.removeEventListener("close", container.onClose);
            }
        }

        container.element.remove();
        this._containers.delete(owner);
    }
}