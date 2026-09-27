// contract
export const contract = {
    description: "Cross-application Bus events",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Bus</h2>

        <p>
            The XShell Bus is application/runtime communication independent of DOM ancestry. This demo uses the current Bus API to register two
            listeners, publish a namespaced event, and remove listeners when they are disabled or when the Page unloads.
        </p>

        <pre x-pre><code>bus.addEventListener(name, listener)
bus.emit(name, detail)
bus.removeEventListener(name, listener)</code></pre>

        <p>
            <button x-on:click="publish">Publish demo event</button>
            <button x-on:click="toggleA">{{ state.listenerA ? 'Disable Listener A' : 'Enable Listener A' }}</button>
            <button x-on:click="toggleB">{{ state.listenerB ? 'Disable Listener B' : 'Enable Listener B' }}</button>
        </p>

        <x-datafields>
            <x-datafield label="Event name"><code>{{ state.eventName }}</code></x-datafield>
            <x-datafield label="Last published message"><span>{{ state.lastPublished }}</span></x-datafield>
        </x-datafields>

        <h3>Listener activity</h3>

        <ul>
            <li x-for="item in state.received" x-key="id"><strong>{{ item.listener }}</strong> received <code>{{ item.type }}</code> — {{ item.detail }}</li>
            <li x-if="!state.received.length">Publish an event to see the listeners receive its detail payload.</li>
        </ul>

        <p>
            A DOM <code>CustomEvent</code> follows a DOM/component interaction path and bubbles through ancestors. A Bus event is delivered through the
            runtime Bus instead. Module <code>contract.events</code> is declarative metadata; it does not automatically enforce Bus payloads.
        </p>
    `,

    state: {
        eventName: "x-demo:advanced:message",
        lastPublished: "",
        listenerA: false,
        listenerB: false,
        received: []
    },

    controller({ state, bus }) {
        const eventName = "x-demo:advanced:message";
        let sequence = 0;
        const listeners = {};
        const record = (listenerName, event) => {
            state.received = [...state.received, {
                id: ++sequence,
                listener: listenerName,
                type: event.type,
                detail: JSON.stringify(event.detail || {})
            }].slice(-12);
        };
        listeners.a = event => record("Listener A", event);
        listeners.b = event => record("Listener B", event);

        return {
            load() {
                // subscribe through the public Bus API
                bus.addEventListener(eventName, listeners.a);
                bus.addEventListener(eventName, listeners.b);
                state.listenerA = true;
                state.listenerB = true;
            },

            publish() {
                // Bus delivery is asynchronous through the runtime channel
                const message = `message ${sequence + 1}`;
                state.lastPublished = message;
                bus.emit(eventName, { message, source: "x-demo advanced Bus page" });
            },

            toggleA() {
                if (state.listenerA) {
                    bus.removeEventListener(eventName, listeners.a);
                } else {
                    bus.addEventListener(eventName, listeners.a);
                }
                state.listenerA = !state.listenerA;
            },

            toggleB() {
                if (state.listenerB) {
                    bus.removeEventListener(eventName, listeners.b);
                } else {
                    bus.addEventListener(eventName, listeners.b);
                }
                state.listenerB = !state.listenerB;
            },

            unload() {
                // remove both retained function references on final Page destruction
                bus.removeEventListener(eventName, listeners.a);
                bus.removeEventListener(eventName, listeners.b);
            }
        };
    }
};
