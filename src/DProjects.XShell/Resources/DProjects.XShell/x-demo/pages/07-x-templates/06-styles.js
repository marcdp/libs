// contract
export const contract = {
    description: "Styles"
};

// export page
export default {
    style: `
        div {--demo-accent:green;}
        .xt-style-box { padding: 1em; border-radius: 0.4em; }
        .xt-style-box.active { box-shadow: 0 0 0.75em var(--demo-accent, currentColor); }
        .xt-style-row { display: flex; gap: 0.75em; align-items: center; flex-wrap: wrap; }
    `,
    template: `
        <p>
            XTemplate styles are structured declarations. Literal styles and named <code>x-style:property</code> bindings become
            <code>VNode.styles</code> and are applied with CSSOM <code>setProperty</code>/<code>removeProperty</code> in the browser.
        </p>

        <h2>Named properties</h2>

        <div
            class="xt-style-box"
            x-style:border="state.border"
            x-style:background-color="state.colored ? state.background : null"
            x-style:--demo-accent="state.accent">
            <p>Hyphenated properties and custom properties keep their CSS spelling.</p>
            <p class="xt-style-row">
                <label><input type="checkbox" x-model="state.active"> Active class</label>
                <label><input type="checkbox" x-model="state.colored"> Background</label>
            </p>
        </div>

        <pre x-pre><code>&lt;div
    x-style:border="state.border"
    x-style:background-color="state.background"
    x-style:--demo-accent="state.accent"&gt;
    Dynamic styles
&lt;/div&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Conditional classes</h2>

        <div class="xt-style-box" x-class:active="state.active">
            <p><code>x-class:active</code> adds or removes the class while retaining authored classes.</p>
        </div>

        <pre x-pre><code>&lt;div class="xt-style-box" x-class:active="state.active"&gt;
    Conditional class
&lt;/div&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Null removes a dynamic declaration</h2>

        <p>
            <label><input type="checkbox" x-model="state.showBorder"> Keep the dynamic border</label>
        </p>
        <div x-style:border="state.showBorder ? state.border : null" class="xt-style-box">
            Uncheck the box to make the dynamic declaration null.
        </div>

        <x-divider></x-divider>

        <h2>Source-order precedence</h2>

        <p>The later declaration wins when the same property appears in literal and named forms.</p>
        <div style="border: 3px solid crimson" x-style:border="state.border" class="xt-style-box">Dynamic border is later, so it wins.</div>
        <div x-style:border="state.border" style="border: 3px solid crimson" class="xt-style-box">Literal border is later, so it wins.</div>

        <pre x-pre><code>&lt;div style="border: 3px solid crimson" x-style:border="state.border"&gt;&lt;/div&gt;
&lt;div x-style:border="state.border" style="border: 3px solid crimson"&gt;&lt;/div&gt;</code></pre>

        <p>
            Only named dynamic styles are supported. Whole-object <code>x-style</code>, dynamic-name <code>x-style:[...]</code>, and runtime
            <code>!important</code> parsing are intentionally not demonstrated.
        </p>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.border = "2px solid var(--demo-accent)";
                state.background = "color-mix(in srgb, var(--demo-accent) 12%, transparent)";
                state.accent = "#3b82f6";
                state.active = true;
                state.colored = true;
                state.showBorder = true;
            }
        };
    }
};
