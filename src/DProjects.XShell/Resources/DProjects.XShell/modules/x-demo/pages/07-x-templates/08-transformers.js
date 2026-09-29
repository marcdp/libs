// contract
export const contract = {
    description: "Transformer Pipelines"
};

// export page
export default {
    template: `
        <p>
            A transformer pipeline starts with an XTemplate expression value and passes it through built-in transformers from left to right.
            It is XTemplate syntax, not a JavaScript method or function call.
        </p>

        <h2>Basic pipeline</h2>

        <p>
            <label>
                Name
                <input x-model="state.name">
            </label>
        </p>
        <p><code>state.name</code>: <strong>{{ state.name }}</strong></p>
        <p><code>state.name | trim</code>: <strong>{{ state.name | trim }}</strong></p>
        <p><code>state.name | trim | upper</code>: <strong>{{ state.name | trim | upper }}</strong></p>
        <p><code>state.name | trim | lower</code>: <strong>{{ state.name | trim | lower }}</strong></p>

        <pre x-pre><code>{{ state.name }}
{{ state.name | trim }}
{{ state.name | trim | upper }}
{{ state.name | trim | lower }}</code></pre>

        <x-divider></x-divider>

        <h2>String transformers</h2>

        <p>
            Predicate transformers return booleans. They receive the original editable value here, so surrounding whitespace still matters.
        </p>
        <p><code>state.name | startsWith('Ada')</code>: <strong>{{ state.name | startsWith('Ada') }}</strong></p>
        <p><code>state.name | endsWith('Lovelace')</code>: <strong>{{ state.name | endsWith('Lovelace') }}</strong></p>
        <p><code>state.name | contains('Love')</code>: <strong>{{ state.name | contains('Love') }}</strong></p>

        <pre x-pre><code>{{ state.name | startsWith('Ada') }}
{{ state.name | endsWith('Lovelace') }}
{{ state.name | contains('Love') }}</code></pre>

        <x-divider></x-divider>

        <h2>Number and presentation transformers</h2>

        <p>
            <label>
                Price
                <input type="number" step="0.001" x-model="state.price">
            </label>
            <label>
                Ratio
                <input type="number" step="0.01" x-model="state.ratio">
            </label>
        </p>
        <p><code>state.price | number(2)</code>: <strong>{{ state.price | number(2) }}</strong></p>
        <p><code>state.ratio | percent(1)</code>: <strong>{{ state.ratio | percent(1) }}</strong></p>
        <p><code>state.price | currency(state.currencyCode | trim | upper, 2)</code>: <strong>{{ state.price | currency(state.currencyCode | trim | upper, 2) }}</strong></p>

        <pre x-pre><code>{{ state.price | number(2) }}
{{ state.ratio | percent(1) }}
{{ state.price | currency(state.currencyCode | trim | upper, 2) }}</code></pre>

        <x-divider></x-divider>

        <h2>Date and time transformers</h2>

        <p>The state uses a fixed ISO-8601 value, so the example does not depend on the current clock or local time zone.</p>
        <p><code>state.createdAt | date('dd/MM/yyyy')</code>: <strong>{{ state.createdAt | date('dd/MM/yyyy') }}</strong></p>
        <p><code>state.createdAt | datetime('yyyy-MM-dd HH:mm')</code>: <strong>{{ state.createdAt | datetime('yyyy-MM-dd HH:mm') }}</strong></p>
        <p><code>state.createdAt | time('HH:mm:ss')</code>: <strong>{{ state.createdAt | time('HH:mm:ss') }}</strong></p>

        <pre x-pre><code>{{ state.createdAt | date('dd/MM/yyyy') }}
{{ state.createdAt | datetime('yyyy-MM-dd HH:mm') }}
{{ state.createdAt | time('HH:mm:ss') }}</code></pre>

        <x-divider></x-divider>

        <h2>JSON transformers</h2>

        <p><code>state.profile | json_stringify</code>: <strong>{{ state.profile | json_stringify }}</strong></p>
        <p><code>(state.jsonText | json_parse).name</code>: <strong>{{ (state.jsonText | json_parse).name }}</strong></p>
        <p><code>state.jsonText | json_parse | json_stringify</code>: <strong>{{ state.jsonText | json_parse | json_stringify }}</strong></p>

        <pre x-pre><code>{{ state.profile | json_stringify }}
{{ (state.jsonText | json_parse).name }}
{{ state.jsonText | json_parse | json_stringify }}</code></pre>

        <x-divider></x-divider>

        <h2>Chaining and nulls</h2>

        <p>
            <code>state.name | trim | upper</code> runs left-to-right: first <code>trim</code>, then <code>upper</code>. Conceptually this is
            <code>upper(trim(state.name))</code>; that notation explains the order but is not valid XTemplate source syntax.
        </p>
        <p>
            The currency example above also shows an argument containing its own pipeline:
            <code>currency(state.currencyCode | trim | upper, 2)</code>.
        </p>
        <p><code>state.nullableValue | trim | upper</code>: <strong>{{ (state.nullableValue | trim | upper) ?? '(null)' }}</strong></p>
        <p><code>state.nullableValue | json_stringify</code>: <strong>{{ state.nullableValue | json_stringify }}</strong></p>
        <p>
            When the current value is null, ordinary transformers are skipped and the pipeline remains null. <code>json_stringify</code> is the
            documented exception: it returns the JSON string <code>null</code>.
        </p>

        <x-divider></x-divider>

        <h2>Pipeline precedence</h2>

        <p>
            <label><input type="checkbox" x-model="state.enabled"> Enabled</label>
        </p>
        <p><code>state.enabled ? 'yes' : 'no' | upper</code>: <strong>{{ state.enabled ? 'yes' : 'no' | upper }}</strong></p>
        <p><code>state.enabled ? ('yes' | upper) : 'no'</code>: <strong>{{ state.enabled ? ('yes' | upper) : 'no' }}</strong></p>
        <p>
            The unparenthesized pipeline applies to the complete conditional. Parentheses are required when only one conditional branch is transformed.
        </p>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.name = "  Ada Lovelace  ";
                state.price = 1234.567;
                state.ratio = 0.42;
                state.currencyCode = " eur ";
                state.enabled = true;
                state.createdAt = "2026-09-24T14:35:06+02:00";
                state.nullableValue = null;
                state.profile = {name: "Ada", enabled: true};
                state.jsonText = '{ "name": "Ada", "enabled": true }';
            }
        };
    }
};
