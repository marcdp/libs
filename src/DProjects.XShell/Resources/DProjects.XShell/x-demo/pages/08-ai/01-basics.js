
 // contract
export const contract = {
    description: "XChat basic usage",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    meta: {
        title: "AI Chat"
    },

    style: `
        .demo {display: flex; flex-direction: column; gap: 1em; width: 100%;}
        .toolbar { display: flex; align-items: center;gap: .75em; }
        x-chat {height: 100%;}
    `,

    template: `
        <h2>Chat</h2>

        <p>
            This example demonstrates the x-chat component using a
            simulated AI assistant. No external AI service is required.
        </p>

        <div class="demo">
            <div class="toolbar">
                <button x-on:click="clear" x-attr:disabled="state.loading">Clear conversation</button>
                <span>{{ state.messages.length }} messages</span>
            </div>

            <div class="chat-container">
                <x-chat ref="abc"
                    x-prop:messages="state.messages"
                    x-prop:loading="state.loading"
                    placeholder="Ask me something..."
                    x-on:send="send"
                    x-on:stop="stop"
                    x-on:retry="retry"
                    autofocus
                ></x-chat>
            </div>
        </div>
    `,

    state: {
        messages: [],
        loading: false
    },

    controller({ state }) {
        let nextId = 0;
        let generation = 0;
        let timer = null;

        const createMessage = (role, content, status = "complete") => ({
            id: String(++nextId),
            role,
            content,
            status
        });

        const updateMessage = (id, changes) => {
            state.messages = state.messages.map(message =>
                message.id === id
                    ? { ...message, ...changes }
                    : message
            );
        };

        const cancelGeneration = () => {
            generation++;
            if (timer !== null) {
                clearTimeout(timer);
                timer = null;
            }
            state.loading = false;
            state.messages = state.messages.map(message => message.status === "streaming" ? { ...message, status: "complete" } : message);
        };

        const getResponse = (content) => {
            const text = content.toLowerCase();

            if (text.includes("hello") || text.includes("hi")) {
                return "Hello! I'm a simulated AI assistant running inside XShell. How can I help you?";
            }

            if (text.includes("xshell")) {
                return "XShell is a modular web application framework. It separates modules, components, pages, resolvers, loaders, services, and rendering engines. The x-chat component is an example of a reusable UI component that remains independent of the AI provider.";
            }

            if (text.includes("component")) {
                return "In XShell, a Component defines reusable UI behavior through a contract and implementation. The x-chat component exposes messages and loading properties, while emitting send, stop, and retry events.";
            }

            if (text.includes("stream")) {
                return "Streaming allows the assistant response to appear progressively. In this demonstration, the Page updates the content of one assistant message over time. The chat component renders those updates without knowing how the response is generated.";
            }

            if (text.includes("help")) {
                return "Try asking me about XShell, components, or streaming. You can also stop a response while it is being generated, retry an assistant message, or clear the conversation.";
            }

            return "This is a simulated AI response to your message: \"" +
                content + "\". In a real application, the Page would delegate the request to an XShell Service, which would communicate with an AI backend and update the conversation as response data arrives.";
        };

        const streamResponse = (content) => {
            cancelGeneration();

            const currentGeneration = generation;
            const response = getResponse(content);
            const words = response.match(/\S+\s*/g) || [];

            const assistant = createMessage(
                "assistant",
                "",
                "streaming"
            );

            state.messages = [...state.messages, assistant];
            state.loading = true;

            let position = 0;
            let output = "";

            const next = () => {
                if (currentGeneration !== generation) return;

                if (position >= words.length) {
                    updateMessage(assistant.id, {
                        content: output.trimEnd(),
                        status: "complete"
                    });

                    state.loading = false;
                    timer = null;
                    return;
                }

                output += words[position++];

                updateMessage(assistant.id, {
                    content: output,
                    status: "streaming"
                });

                timer = setTimeout(next, 65);
            };

            timer = setTimeout(next, 250);
        };

        return {
            load() {
                state.messages = [
                    createMessage(
                        "assistant",
                        "Welcome to the XShell Chat demo! Ask me about XShell, components, or streaming."
                    )
                ];
            },

            mount(){
                //const page = getPage();
                //alert(page);
            },

            send({ event }) {
                const content = event.detail?.content?.trim();
                if (!content || state.loading) return;

                state.messages = [
                    ...state.messages,
                    createMessage("user", content)
                ];

                streamResponse(content);
            },

            stop() {
                cancelGeneration();
            },

            retry({ event }) {
                if (state.loading) return;

                const messageId = String(event.detail?.messageId ?? "");
                const index = state.messages.findIndex(
                    message => message.id === messageId
                );

                if (index < 0) return;

                // Find the most recent user message before this response.
                const userMessage = state.messages
                    .slice(0, index)
                    .reverse()
                    .find(message => message.role === "user");

                if (!userMessage) return;

                // Remove the retried assistant response and later messages.
                state.messages = state.messages.slice(0, index);

                streamResponse(userMessage.content);
            },

            clear() {
                cancelGeneration();
                state.messages = [];
            },

            unload() {
                cancelGeneration();
            }
        };
    }
};
