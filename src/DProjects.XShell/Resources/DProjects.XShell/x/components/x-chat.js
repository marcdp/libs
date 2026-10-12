// contract
export const contract = {
    description: "Reusable conversation interface with streaming support.",
    events: {
        send: {
            description: "Raised when the user submits a message.",
            detail: {
                content: { type: "string" }
            }
        },
        stop: {
            description: "Raised when the user requests generation to stop.",
            detail: {}
        },
        retry: {
            description: "Raised when the user requests a response retry.",
            detail: {
                messageId: { type: "string" }
            }
        }
    },
    properties: {
        messages: {type: "array", default: [], state: true, description: "Conversation messages."},
        value: {type: "string", default: "", state: true, description: "Current composer text."},
        loading: { type: "boolean", default: false, attribute: true, state: true, description: "Whether a response is being generated."},
        disabled: { type: "boolean", default: false, attribute: true, state: true, description: "Disables message submission."},
        placeholder: { type: "string", default: "Send a message...", attribute: true, state: true, description: "Composer placeholder."},
        autofocus: { type: "boolean", default: false, attribute: true, state: true, description: "Automatically focuses the message composer on mount." }
    },
    methods: {
        setFocus: {
            description: "Focuses the message composer."
        },
        clear: {
            description: "Clears the composer text."
        },
        scrollToBottom: {
            description: "Scrolls to the newest message."
        }
    },
    slots: {}
};

// implementation
export default {
    meta: {
        renderEngine: "x",
        stateEngine: "proxy"
    },

    style: `
        :host {
            display: flex;
            flex-direction: column;
            width: 100%;
            height: 100%;
            min-height: 0;
            color: var(--x-color-text);
        }

        .chat {
            display: flex;
            flex-direction: column;
            flex: 1;
            min-height: 0;
            width: 100%;
        }

        .messages {
            flex: 1;
            min-height: 0;
            overflow-y: auto;
            overscroll-behavior: contain;
            padding: 1.5em 1em;
            scrollbar-gutter: stable;
        }

        .messages-inner {
            display: flex;
            flex-direction: column;
            gap: 1.25em;
            width: 100%;
            margin: 0 auto;
        }

        .message {
            display: flex;
            flex-direction: column;
            gap: .35em;
            min-width: 0;
        }

        .message.user {
            align-items: flex-end;
        }

        .message.assistant,
        .message.system {
            align-items: flex-start;
        }

        .message-role {
            color: var(--x-color-gray);
            font-size: var(--x-font-size-small, .85em);
            font-weight: 600;
        }

        .message-body {
            box-sizing: border-box;
            max-width: 90%;
            min-width: 0;
            padding: .75em 1em;
            border-radius: var(--x-chat-message-border-radius, 1em);
            background: var(--x-chat-assistant-background, transparent);
            overflow-wrap: anywhere;
        }

        .message.user .message-body {
            background: var(--x-chat-user-background);            
        }

        .message.system .message-body {
            color: var(--x-color-gray);
            font-size: var(--x-font-size-small, .85em);
        }

        .message-content {
            white-space: pre-wrap;
            line-height: 1.6;
            user-select: text;
        }

        .message-actions {
            display: flex;
            gap: .5em;
            align-items: center;
        }

        .message-actions button {
            border: 0;
            background: transparent;
            color: var(--x-color-gray);
            cursor: pointer;
            font: inherit;
            font-size: .85em;
            padding: .25em;
        }

        .message-actions button:hover {
            color: var(--x-color-text);
        }

        .empty {
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            color: var(--x-color-gray);
            text-align: center;
            padding: 2em;
        }

        .streaming {
            display: inline-flex;
            align-items: center;
            gap: .2em;
            margin-left: .3em;
            vertical-align: middle;
        }

        .streaming span {
            width: .3em;
            height: .3em;
            border-radius: 50%;
            background: currentColor;
            opacity: .25;
            animation: chat-pulse 1.2s infinite;
        }

        .streaming span:nth-child(2) {
            animation-delay: .2s;
        }

        .streaming span:nth-child(3) {
            animation-delay: .4s;
        }

        @keyframes chat-pulse {
            0%, 60%, 100% { opacity: .25; }
            30% { opacity: 1; }
        }

        .composer-container {
            flex-shrink: 0;
            padding: .75em 1em 1em;
            background: var(--x-chat-composer-background, transparent);
        }

        .composer {
            display: flex;
            align-items: flex-end;
            gap: .5em;
            margin: 0 auto;
            padding: .5em;
            border: var(--x-datafield-border);
            border-radius: var(--x-datafield-border-radius);
            background: var(--x-datafield-background, transparent);
        }

        .composer:focus-within {
            border-color: var(--x-color-primary);
        }

        .composer textarea {
            flex: 1;
            box-sizing: border-box;
            min-width: 0;
            min-height: 2.5em;
            max-height: 12em;
            padding: .45em;
            resize: none;
            border: 0;
            outline: none;
            background: transparent;
            color: inherit;
            font: inherit;
            line-height: 1.5;
            overflow-y: auto;
            
        }

        .composer button {
            flex-shrink: 0;
            min-width: 3.5em;
            min-height: 2.5em;
            padding: .4em .7em;
            border: 0;
            border-radius: .65em;
            background: var(--x-color-primary);
            color: white;
            font: inherit;
            cursor: pointer;
        }

        .composer button:disabled {
            opacity: .4;
            cursor: default;
        }

        .composer-hint {
            margin: .5em auto 0;
            text-align: center;
            color: var(--x-color-gray);
            font-size: .75em;
        }

        @media (max-width: 768px) {
            .messages {
                padding: 1em .75em;
            }

            .composer-container {
                padding: .5em;
            }

            .message-body {
                max-width: 95%;
            }
        }

        @media (prefers-reduced-motion: reduce) {
            .streaming span {
                animation: none;
                opacity: .6;
            }
        }
    `,

    template: `
        <div class="chat">
            <div class="messages" x-on:scroll="handleScroll">
                <div class="messages-inner">

                    <div class="empty" x-if="!state.messages.length">
                        <div>Start a conversation</div>
                    </div>

                    <div
                        class="message"
                        x-for="message in state.messages"
                        x-key="id"
                        x-attr:class="'message ' + message.role"
                    >
                        <div class="message-role">
                            {{ message.role }}
                        </div>

                        <div class="message-body">
                            <div class="message-content" x-text="message.content"></div>

                            <div
                                class="streaming"
                                x-if="message.status == 'streaming'"
                                aria-label="Generating response"
                            >
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                        </div>

                        <div
                            class="message-actions"
                            x-if="message.role == 'assistant' && message.status != 'streaming'"
                        >
                            <button
                                type="button"
                                x-on:click="copyMessage"
                            >Copy</button>

                            <button
                                type="button"
                                x-on:click="retryMessage"
                            >Retry</button>
                        </div>
                    </div>

                </div>
            </div>

            <div class="composer-container">
                <div class="composer">
                    <textarea
                        rows="1"
                        x-model="state.value"
                        x-attr:placeholder="state.placeholder"
                        x-attr:disabled="state.disabled"
                        x-on:input="composerInput"
                        x-on:keydown="composerKeydown"
                    ></textarea>
                    <button type="button" x-if="!state.loading" x-attr:disabled="state.disabled || !state.value" x-on:click="send">Send</button>
                    <button type="button" x-else x-on:click="stop">Stop</button>
                </div>

                <div class="composer-hint">
                    Enter to send · Shift+Enter for a new line
                </div>
            </div>
        </div>
    `,

    state: {
        autofocus: false,
        stickToBottom: true
    },

    controller({ state, events, host, timer }) {
        let scrollScheduled = false;
        let observer = null;

        const getMessagesContainer = () => host.shadowRoot.querySelector(".messages");
        const getComposer = () => host.shadowRoot.querySelector("textarea");
        const emit = (type, detail = {}) => {
            host.dispatchEvent(new CustomEvent(type, {detail, bubbles: true, composed: true }));
        };
        const scheduleScroll = () => {
            if (scrollScheduled || !state.stickToBottom) return;
            scrollScheduled = true;
            requestAnimationFrame(() => {
                scrollScheduled = false;
                if (state.stickToBottom) {
                    const container = getMessagesContainer();
                    if (container) {
                        container.scrollTop = container.scrollHeight;
                    }
                }
            });
        };

        return {
            load() {
                // load
                events.on(state, "change:messages", "messagesChanged");
                // Observe message content growth, including streaming.
                const content = host.shadowRoot.querySelector(".messages-inner");
                if (content && typeof ResizeObserver !== "undefined") {
                    observer = new ResizeObserver(() => scheduleScroll());
                    observer.observe(content);
                }
                // scroll
                scheduleScroll();
            },
            async mount(args) {
                //mount
                if (state.autofocus) {
                    timer.setTimeout(25, () => {
                        let element = host.shadowRoot.querySelector("textarea");
                        if (element && element.focus) element.focus();
                    });
                }
            },

            unload() {
                observer?.disconnect();
                observer = null;
            },

            messagesChanged() {
                scheduleScroll();
            },

            handleScroll() {
                const container = getMessagesContainer();
                const distance = container.scrollHeight - container.scrollTop - container.clientHeight;
                state.stickToBottom = distance < 80;
            },

            composerInput({ event }) {
                const textarea = event.target;
                textarea.style.height = "auto";
                textarea.style.height = Math.min(textarea.scrollHeight, 192) + "px";
                state.value = textarea.value;
            },

            composerKeydown({ event }) {
                if (event.key === "Enter" && !event.shiftKey && !event.isComposing) {
                    event.preventDefault();
                    this.send();
                }
            },

            send() {
                if (state.disabled || state.loading) return;

                const content = state.value.trim();
                if (!content) return;

                state.value = "";
                state.stickToBottom = true;

                const textarea = getComposer();
                if (textarea) {
                    textarea.value = "";
                    textarea.style.height = "auto";
                }

                emit("send", { content });
                scheduleScroll();
            },

            stop() {
                if (!state.loading) return;
                emit("stop");
            },

            copyMessage({ event }) {
                const element = event.target.closest(".message");
                if (!element) return;

                const index = Array.from(
                    element.parentElement.children
                ).filter(item => item.classList.contains("message"))
                 .indexOf(element);

                const message = state.messages[index];
                if (!message) return;

                navigator.clipboard?.writeText(message.content || "");
            },

            retryMessage({ event }) {
                const element = event.target.closest(".message");
                if (!element) return;

                const index = Array.from(
                    element.parentElement.children
                ).filter(item => item.classList.contains("message"))
                 .indexOf(element);

                const message = state.messages[index];
                if (!message) return;

                emit("retry", { messageId: String(message.id) });
            },

            setFocus() {
                getComposer()?.focus();
            },

            clear() {
                state.value = "";
                const textarea = getComposer();
                if (textarea) {
                    textarea.value = "";
                    textarea.style.height = "auto";
                }
            },

            scrollToBottom() {
                state.stickToBottom = true;
                scheduleScroll();
            }
        };
    }
};