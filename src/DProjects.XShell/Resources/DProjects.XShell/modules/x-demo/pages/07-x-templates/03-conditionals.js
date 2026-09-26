// contract
export const contract = {
    description: "Conditionals"
};

// export page
export default {
    template: `
        <p>
            Structural conditionals choose which nodes exist in the rendered structure. Visibility conditionals keep their element and control its
            <code>hidden</code> attribute.
        </p>

        <h2>Interactive state</h2>

        <p>
            <label><input type="checkbox" x-model="state.visible"> Show the details</label>
        </p>
        <p>Status: <strong>{{ state.status }}</strong></p>
        <p>
            <button x-on:click="ready">Ready</button>
            <button x-on:click="working">Working</button>
            <button x-on:click="other">Other</button>
        </p>

        <x-divider></x-divider>

        <h2>x-if, x-elseif, and x-else</h2>

        <div x-if="state.status == 'ready'">The structural branch says: Ready.</div>
        <div x-elseif="state.status == 'working'">The structural branch says: Working.</div>
        <div x-else>The structural branch says: another state.</div>

        <pre x-pre><code>&lt;div x-if="state.status == 'ready'"&gt;Ready&lt;/div&gt;
&lt;div x-elseif="state.status == 'working'"&gt;Working&lt;/div&gt;
&lt;div x-else&gt;Other state&lt;/div&gt;</code></pre>

        <x-divider></x-divider>

        <h2>x-show</h2>

        <div class="conditional-card" x-show="state.visible">
            This element remains in the rendered structure while <code>state.visible</code> controls its <code>hidden</code> attribute.
        </div>

        <pre x-pre><code>&lt;div x-show="state.visible"&gt;
    Visibility changes without removing the node.
&lt;/div&gt;</code></pre>

        <p>
            Use one primary structural directive per element: <code>x-if</code>, <code>x-elseif</code>, <code>x-else</code>, <code>x-for</code>,
            <code>x-recursive</code>, or <code>x-once</code>.
        </p>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.visible = true;
                state.status = "ready";
            },
            ready() {
                state.status = "ready";
            },
            working() {
                state.status = "working";
            },
            other() {
                state.status = "paused";
            }
        };
    }
};
