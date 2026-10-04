import parser from "../utils/markdown.js";

// contract
export const contract = {
    description: "Renders Markdown text or Markdown loaded from a source URL.",
    events: {},
    properties: {
        value: {type:"string", default:"", attribute:true, state:true, description:"Markdown content."},
        src:   {type:"string", default:"", attribute:true, state:true, description:"Normal browser URL to fetch Markdown from; relative links and resources use this base."}
    },
    methods: {},
    slots: {
        "":{
            description: "Default slot."
        }
    }
};

// implementation
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `,
    state: {
    },
    controller({ state, events, loader, host }) {
        let revision = 0;

        async function render(value) {
            // parse once and process DOM, including nested template contents
            const current = ++revision;
            const doc = new DOMParser().parseFromString(parser(value), "text/html");
            const base = state.src ? new URL(state.src, document.baseURI) : null;
            const origin = new URL(document.baseURI).origin;
            const componentIds = new Set();
            function inspect(root) {
                for (const element of root.querySelectorAll("*")) {
                    if (element.localName.includes("-")) componentIds.add("component:" + element.localName);
                    const attribute = element.matches("a[href]") ? "href"
                        : element.matches("img[src], source[src], video[src], audio[src]") ? "src" : null;
                    const url = attribute && element.getAttribute(attribute);
                    if (base && url && !url.startsWith("#") && !url.startsWith("/") && !/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(url)) {
                        const resolved = new URL(url, base);
                        element.setAttribute(attribute, resolved.origin === origin
                            ? resolved.pathname + resolved.search + resolved.hash : resolved.href);
                    }
                    if (element.localName === "template") inspect(element.content);
                }
            }
            inspect(doc.body);
            // load embedded components before committing the latest rendered content
            await loader.load([...componentIds]);
            if (current === revision) host.innerHTML = doc.body.innerHTML;
        }

        return {
            load() {
                // both input modes share the same Markdown rendering path
                events.on(state, "change:value", event => render(event.newValue));
                events.on(state, "change:src", async (event) => {
                    const current = ++revision;
                    const src = event.newValue;
                    if (!src) return;
                    // browser fetch allows the Service Worker to virtualize asset URLs
                    const response = await fetch(src);
                    const value = response.ok ? await response.text() : `${response.status} - ${response.statusText}: ${src}`;
                    if (current !== revision) return;
                    // identical documents still need URL normalization against the new source
                    if (state.value === value) await render(value);
                    else state.value = value;
                });
            }
        };
    }
};
