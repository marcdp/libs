// contract
export const contract = {
    description: "Displays a compact preview of a value and opens the full value in a new browser tab.",
    events: {
        open: {
            description: "Raised when the full value is opened.",
            detail: {
                value: {type:"any"}
            }
        }
    },
    properties: {
        value: {
            type: "any",
            default: null,
            attribute: false,
            state: true,
            description: "Value to preview."
        }
    },
    methods: {
        open: {
            description: "Opens the full value in a new browser tab."
        }
    },
    slots: {}
};


// implementation
export default {
    style: `
        :host {
            min-width:0;
            max-width:100%;
        }

        .preview {
            display:flex;
            align-items:center;
            gap:.25em;
            min-width:0;
            max-width:100%;
            overflow:hidden;
        }

        .text {
            flex:1 1 auto;
            min-width:0;
            overflow:hidden;
        }

        .image,
        .video {
            display:block;
            flex:none;
            max-width:6em;
            max-height:3em;
            object-fit:contain;
        }

        x-button {
            flex:none;
        }
    `,

    template: `
        <div class="preview">
            <img
                x-if="state.type == 'image'"
                class="image"
                x-attr:src="state.src">

            <video
                x-elseif="state.type == 'video'"
                class="video"
                x-attr:src="state.src">
            </video>

            <x-truncate
                x-else
                class="text"
                lines="2"
                x-attr:text="state.text">
            </x-truncate>

            <x-button
                x-if="state.value != null"
                class="plain round"
                icon="x-open-in-new"
                x-on:click="open">
            </x-button>
        </div>
    `,

    state: {
        type: "empty",
        text: "",
        src: null
    },

    controller({state, events, host}) {
        let previewObjectUrl = null;

        function formatSize(bytes) {
            if (bytes < 1024) {
                return `${bytes} B`;
            }

            if (bytes < 1024 * 1024) {
                return `${(bytes / 1024).toFixed(1)} KB`;
            }

            return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
        }

        function formatJsonPreview(value) {
            try {
                const text = JSON.stringify(value);

                if (text.length > 255) {
                    return text.substring(0, 255) + "...";
                }

                return text;
            } catch {
                return "[Object]";
            }
        }

        function createViewerBlob(value) {
            if (value instanceof Blob) {
                return value;
            }

            if (value instanceof Date) {
                return new Blob(
                    [value.toISOString()],
                    {type:"text/plain;charset=utf-8"}
                );
            }

            if (Array.isArray(value) || typeof value === "object") {
                let content;

                try {
                    content = JSON.stringify(value, null, 2);
                } catch {
                    content = String(value);
                }

                return new Blob(
                    [content],
                    {type:"application/json;charset=utf-8"}
                );
            }

            return new Blob(
                [String(value)],
                {type:"text/plain;charset=utf-8"}
            );
        }

        return {
            load() {
                events.on(state, "change:value", "refresh");
                this.refresh();
            },

            unload() {
                this.releasePreviewObjectUrl();
            },

            releasePreviewObjectUrl() {
                if (!previewObjectUrl) {
                    return;
                }

                URL.revokeObjectURL(previewObjectUrl);
                previewObjectUrl = null;
            },

            refresh() {
                this.releasePreviewObjectUrl();

                const value = state.value;

                state.type = "empty";
                state.text = "";
                state.src = null;

                if (value == null) {
                    return;
                }

                if (value instanceof Date) {
                    state.type = "date";
                    state.text = value.toISOString();
                    return;
                }

                if (value instanceof Blob) {
                    const mimeType = value.type || "";

                    if (mimeType.startsWith("image/")) {
                        previewObjectUrl = URL.createObjectURL(value);

                        state.type = "image";
                        state.src = previewObjectUrl;
                        return;
                    }

                    if (mimeType.startsWith("video/")) {
                        previewObjectUrl = URL.createObjectURL(value);

                        state.type = "video";
                        state.src = previewObjectUrl;
                        state.text = `Video · ${formatSize(value.size)}`;
                        return;
                    }

                    if (mimeType === "application/pdf") {
                        state.type = "pdf";
                        state.text = `PDF · ${formatSize(value.size)}`;
                        return;
                    }

                    state.type = "blob";
                    state.text = `${mimeType || "Blob"} · ${formatSize(value.size)}`;
                    return;
                }

                if (Array.isArray(value)) {
                    state.type = "json";
                    state.text = formatJsonPreview(value);
                    return;
                }

                if (typeof value === "object") {
                    state.type = "json";
                    state.text = formatJsonPreview(value);
                    return;
                }

                state.type = "text";
                state.text = String(value);
            },

            open() {
                const value = state.value;

                if (value == null) {
                    return;
                }

                host.dispatchEvent(new CustomEvent("open", {
                    detail: {
                        value
                    },
                    bubbles: true,
                    composed: false
                }));

                const blob = createViewerBlob(value);
                const url = URL.createObjectURL(blob);
                const newWindow = window.open(url, "_blank");

                if (!newWindow) {
                    URL.revokeObjectURL(url);
                    return;
                }

                setTimeout(() => {
                    URL.revokeObjectURL(url);
                }, 60_000);
            }
        };
    }
};