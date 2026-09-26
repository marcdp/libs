// contract
export const contract = {
    description: "X Templates"
};

// export page
export default {
    template: `
        <p>
            XTemplate is the declarative template language used by XShell's <code>x</code> render engine. These pages are executable examples:
            state is evaluated by the template, compiled into a VNode, and reconciled with the DOM.
        </p>

        <h2>The mental model</h2>

        <p>
            The normal flow is <strong>state &rarr; template expressions &rarr; VNode &rarr; DOM</strong>. XTemplate expressions are intentionally
            restricted rather than arbitrary JavaScript. Templates are compiled server-side, and the browser consumes precompiled render functions.
        </p>

        <p>Hello, {{ state.name }}</p>

        <pre x-pre><code>&lt;p&gt;Hello, {{ state.name }}&lt;/p&gt;</code></pre>

        <p>
            Directives bind state to attributes, properties, styles, structure, and named events. Browser styles use structured CSSOM declarations,
            not runtime mutation of an inline style string.
        </p>

        <x-divider></x-divider>

        <h2>Roadmap</h2>

        <ul>
            <li><strong>Basics</strong> &mdash; interpolation, expressions, text, and transformers.</li>
            <li><strong>Bindings</strong> &mdash; attributes, properties, and dynamic names.</li>
            <li><strong>Conditionals</strong> &mdash; structural rendering and visibility.</li>
            <li><strong>Loops</strong> &mdash; collections, keys, and recursive trees.</li>
            <li><strong>Events and model</strong> &mdash; commands and two-way controls.</li>
            <li><strong>Styles</strong> &mdash; structured styles and conditional classes.</li>
            <li><strong>Advanced</strong> &mdash; one-time, preformatted, and trusted raw content.</li>
        </ul>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.name = "XTemplate";
            }
        };
    }
};
