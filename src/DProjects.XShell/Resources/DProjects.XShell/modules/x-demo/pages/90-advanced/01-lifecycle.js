// contract
export const contract = {
    description: "Definition-based Page lifecycle",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Lifecycle</h2>

        <p>
            Definition-based Pages and Components have two ownership scopes. <code>load</code> and <code>unload</code> belong to one instance;
            <code>mount</code> and <code>unmount</code> belong to each connection or render-engine attachment.
        </p>

        <pre x-pre><code>load       once per instance
mount      once per connection
unmount    once per disconnection
mount      again when the same instance reconnects
unload     once when the instance is destroyed</code></pre>

        <p>
            The child below is another real <code>x-page</code> instance of this Page. Its lifecycle handlers publish a namespaced Bus event, so the
            log records actual XShell callbacks rather than manually invoking controller methods.
        </p>

        <p>
            <button x-on:click="remountChild">Disconnect + reconnect</button>
            <button x-on:click="destroyChild">Destroy + recreate</button>
            <button x-on:click="clearLog">Clear log</button>
        </p>

        <p>Child source: <code>{{ state.childSrc }}</code></p>

        <div class="lifecycle-child" x-if="!state.isChild">
            <x-page data-lifecycle-child layout="embed" x-attr:src="state.childSrc"></x-page>
        </div>

        <h3>Observed child lifecycle</h3>

        <ul>
            <li x-for="entry in state.log" x-key="id"><code>{{ entry.phase }}</code> — {{ entry.detail }}</li>
            <li x-if="!state.log.length">Waiting for the child Page to load.</li>
        </ul>

        <p>
            <code>unmount != unload</code>: temporary DOM disconnection removes mount-owned output, but the Page controller, state, timers, events, and
            other disposables remain available until final unload.
        </p>
    `,

    state: {
        childSrc: "",
        isChild: false,
        log: []
    },

    controller({ state, page, query, bus }) {
        const eventName = "x-demo:advanced:lifecycle";
        let sequence = 0;
        const onLifecycle = event => {
            const detail = event.detail || {};
            if (detail.role !== "child") return;
            state.log = [...state.log, {
                id: ++sequence,
                phase: detail.phase,
                detail: detail.src || "child Page"
            }];
        };

        bus.addEventListener(eventName, onLifecycle);

        return {
            load() {
                // configure the outer Page and the nested lifecycle probe
                state.isChild = query.get("role") === "child";
                state.childSrc = state.isChild ? "" : `${page.src.split("?")[0]}?role=child`;
                if (state.isChild) bus.emit(eventName, { phase: "load", role: "child", src: page.src });
            },

            mount() {
                // report the real Page mount callback
                if (state.isChild) bus.emit(eventName, { phase: "mount", role: "child", src: page.src });
            },

            unmount() {
                // report the real Page unmount callback
                if (state.isChild) bus.emit(eventName, { phase: "unmount", role: "child", src: page.src });
            },

            unload() {
                // report before disposing this Page's listener
                if (state.isChild) bus.emit(eventName, { phase: "unload", role: "child", src: page.src });
                bus.removeEventListener(eventName, onLifecycle);
            },

            remountChild() {
                // DOM reconnection exercises x-page connected/disconnected callbacks
                const child = page.host.querySelector("[data-lifecycle-child]");
                const parent = child?.parentElement;
                if (!child || !parent) return;
                child.remove();
                parent.appendChild(child);
            },

            async destroyChild() {
                // removePage is the XShell destruction path and therefore reaches unload
                const child = page.host.querySelector("[data-lifecycle-child]");
                const parent = child?.parentElement;
                if (!child || !parent) return;
                await child.removePage();
                const replacement = document.createElement("x-page");
                replacement.setAttribute("data-lifecycle-child", "");
                replacement.setAttribute("layout", "embed");
                replacement.setAttribute("src", state.childSrc);
                parent.appendChild(replacement);
            },

            clearLog() {
                state.log = [];
            }
        };
    }
};
