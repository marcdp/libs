// contract
export const contract = {
    description: "Loops"
};

// export page
export default {
    template: `
        <p>
            <code>x-for</code> normalizes a collection and renders one copy of its element per item. A key gives each item stable identity during
            reconciliation.
        </p>

        <h2>Keyed collection with an index</h2>

        <ul>
            <li x-for="(item,index) in state.items" x-key="id">
                {{ index }} &mdash; {{ item.label }}
            </li>
        </ul>

        <pre x-pre><code>&lt;li x-for="(item,index) in state.items" x-key="id"&gt;
    {{ index }} &mdash; {{ item.label }}
&lt;/li&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Collection normalization</h2>

        <p>Number <code>3</code> becomes the sequence 1, 2, 3:</p>
        <ol>
            <li x-for="value in state.numberCollection">{{ value }}</li>
        </ol>

        <p>Strings iterate by Unicode code point:</p>
        <ul>
            <li x-for="character in state.stringCollection">{{ character }}</li>
        </ul>

        <p>A <code>null</code> collection becomes empty and renders no list items:</p>
        <ul>
            <li x-for="item in state.nullCollection">This is not rendered</li>
        </ul>

        <pre x-pre><code>&lt;li x-for="value in state.numberCollection"&gt;{{ value }}&lt;/li&gt;
&lt;li x-for="character in state.stringCollection"&gt;{{ character }}&lt;/li&gt;
&lt;li x-for="item in state.nullCollection"&gt;Empty&lt;/li&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Recursive tree</h2>

        <ul>
            <li x-recursive="(item,index,indexAbsolute) in state.tree" x-key="id" x-recursive-wrapper="ul">
                <span>{{ indexAbsolute }}. {{ item.label }}</span>
            </li>
        </ul>

        <pre x-pre><code>&lt;li
    x-recursive="(item,index,indexAbsolute) in state.tree"
    x-key="id"
    x-recursive-wrapper="ul"&gt;
    {{ indexAbsolute }}. {{ item.label }}
&lt;/li&gt;</code></pre>

        <p>
            Recursive children use the fixed <code>children</code> member. A missing or null child collection simply ends that branch.
        </p>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.items = [
                    {id: "a", label: "Alpha"},
                    {id: "b", label: "Beta"},
                    {id: "c", label: "Gamma"}
                ];
                state.numberCollection = 3;
                state.stringCollection = "A😀B";
                state.nullCollection = null;
                state.tree = [
                    {
                        id: "root",
                        label: "Root",
                        children: [
                            {id: "child-a", label: "Child A", children: []},
                            {id: "child-b", label: "Child B", children: [
                                {id: "grandchild", label: "Grandchild", children: null}
                            ]}
                        ]
                    }
                ];
            }
        };
    }
};
