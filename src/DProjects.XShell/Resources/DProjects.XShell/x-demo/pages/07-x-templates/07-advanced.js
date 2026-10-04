// contract
export const contract = {
    description: "Advanced"
};

// export page
export default {
    template: `
        <p>
            These features mark boundaries between XTemplate and normal controller JavaScript. The template can express rendering rules, while the
            controller owns state changes and trusted application values.
        </p>

        <h2>x-once</h2>

        <p>State value: {{ state.value }}</p>
        <button x-on:click="changeValue">Change state</button>
        <div x-once>
            This subtree rendered once with: {{ state.value }}
        </div>

        <pre x-pre><code>&lt;div x-once&gt;
    This subtree rendered once with: {{ state.value }}
&lt;/div&gt;</code></pre>

        <p>
            The surrounding value changes, but the <code>x-once</code> subtree preserves its first DOM render.
        </p>

        <x-divider></x-divider>

        <h2>x-pre</h2>

        <pre x-pre><code>&lt;button x-on:click="not-a-command"&gt;{{ literalBraces }}&lt;/button&gt;</code></pre>

        <p>
            The directive-looking markup and interpolation-looking text above are literal because <code>x-pre</code> makes its child subtree opaque
            to XTemplate compilation.
        </p>

        <x-divider></x-divider>

        <h2>x-html</h2>

        <p>
            <code>x-html</code> inserts raw HTML. This example uses a fixed local trusted string; never pass untrusted or unsanitized user content
            to this directive.
        </p>
        <div x-html="state.trustedHtml"></div>

        <pre x-pre><code>&lt;div x-html="state.trustedHtml"&gt;&lt;/div&gt;</code></pre>

        <x-divider></x-divider>

        <h2>x-children</h2>

        <p>
            <code>x-children</code> is for actual DOM nodes or arrays of DOM nodes, not serializable template values. It is used safely by XShell
            components such as <code>x-icon</code>, but this page does not manufacture DOM nodes in state just to make an artificial example.
        </p>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.value = "first render";
                state.trustedHtml = "<strong>Trusted local HTML</strong> with <em>markup</em>.";
            },
            changeValue() {
                state.value = "changed at " + new Date().toLocaleTimeString();
            }
        };
    }
};
