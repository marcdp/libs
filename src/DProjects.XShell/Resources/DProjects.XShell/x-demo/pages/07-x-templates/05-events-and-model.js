// contract
export const contract = {
    description: "Events And Model"
};

// export page
export default {
    template: `
        <p>
            Event bindings dispatch named controller commands. They do not execute arbitrary inline JavaScript. <code>x-model</code> reads a control
            value into the DOM and writes changes back to an assignable state target.
        </p>

        <h2>Named commands</h2>

        <p>Count: {{ state.count }}</p>
        <button x-on:click="increment">Increment</button>
        <button x-on:click="reset">Reset</button>

        <pre x-pre><code>&lt;button x-on:click="increment"&gt;Increment&lt;/button&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Text and nested model targets</h2>

        <p>
            <label>
                Name
                <input x-model="state.form.name">
            </label>
        </p>
        <p>Hello {{ state.form.name }}</p>

        <pre x-pre><code>&lt;input x-model="state.form.name"&gt;
&lt;p&gt;Hello {{ state.form.name }}&lt;/p&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Number and checkbox models</h2>

        <p>
            <label>
                Count
                <input type="number" x-model="state.count">
            </label>
            <label>
                <input type="checkbox" x-model="state.enabled">
                Enabled: {{ state.enabled }}
            </label>
        </p>

        <pre x-pre><code>&lt;input type="number" x-model="state.count"&gt;
&lt;input type="checkbox" x-model="state.enabled"&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Radio scalar comparison</h2>

        <p>
            The model starts as the number <code>1</code>. Radio values are strings, and radio checked-state comparison uses their scalar-string
            forms, so the first radio is selected.
        </p>
        <label><input type="radio" name="choice" value="1" x-model="state.choice"> One</label>
        <label><input type="radio" name="choice" value="2" x-model="state.choice"> Two</label>
        <p>Choice: {{ state.choice }}</p>

        <pre x-pre><code>&lt;input type="radio" value="1" x-model="state.choice"&gt;
&lt;input type="radio" value="2" x-model="state.choice"&gt;</code></pre>

        <p>
            Model updates use the current <code>change</code>-based behavior. Multiple-select model binding is intentionally not shown because it
            remains unresolved/unsupported.
        </p>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.count = 1;
                state.enabled = true;
                state.choice = 1;
                state.form = {name: "Ada"};
            },
            increment() {
                state.count += 1;
            },
            reset() {
                state.count = 1;
            }
        };
    }
};
