// contract
export const contract = {
    description: "Basics"
};

// export page
export default {
    template: `
        <p>
            Expressions produce text from state. They support member access, arithmetic, boolean operators, null-coalescing, conditionals, and the
            restricted transformer pipeline; they do not call arbitrary JavaScript functions or methods.
        </p>

        <h2>Interpolation and member access</h2>

        <p>Hello, {{ state.user.name }}.</p>
        <p>First item: {{ state.items[0] }}</p>

        <pre x-pre><code>&lt;p&gt;Hello, {{ state.user.name }}.&lt;/p&gt;
&lt;p&gt;First item: {{ state.items[0] }}&lt;/p&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Arithmetic and boolean expressions</h2>

        <p>Total: {{ state.price * state.quantity | number(2) }}</p>
        <p x-text="state.enabled ? 'Enabled' : 'Disabled'"></p>
        <p>Access: {{ state.user.active && state.enabled ? 'available' : 'paused' }}</p>

        <pre x-pre><code>&lt;p&gt;Total: {{ state.price * state.quantity | number(2) }}&lt;/p&gt;
&lt;p x-text="state.enabled ? 'Enabled' : 'Disabled'"&gt;&lt;/p&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Null-coalescing and text</h2>

        <p>Nickname: {{ state.nickname ?? 'Anonymous' }}</p>
        <p x-text="state.message"></p>

        <pre x-pre><code>&lt;p&gt;{{ state.nickname ?? 'Anonymous' }}&lt;/p&gt;
&lt;p x-text="state.message"&gt;&lt;/p&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Transformers</h2>

        <p>Name: {{ state.name | trim | upper }}</p>
        <p>Code starts with X: {{ state.code | startsWith('X') }}</p>
        <p>Price: {{ state.price | currency('EUR', 2) }}</p>

        <pre x-pre><code>&lt;p&gt;{{ state.name | trim | upper }}&lt;/p&gt;
&lt;p&gt;{{ state.code | startsWith('X') }}&lt;/p&gt;
&lt;p&gt;{{ state.price | currency('EUR', 2) }}&lt;/p&gt;</code></pre>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.user = {name: "Ada", active: true};
                state.items = ["Alpha", "Beta"];
                state.price = 12.5;
                state.quantity = 3;
                state.enabled = true;
                state.nickname = null;
                state.message = "x-text evaluates a value and writes text content.";
                state.name = "  Ada Lovelace  ";
                state.code = "XTL-01";
            }
        };
    }
};
