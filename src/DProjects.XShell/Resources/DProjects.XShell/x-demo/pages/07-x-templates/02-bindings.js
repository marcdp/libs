// contract
export const contract = {
    description: "Bindings"
};

// export page
export default {
    template: `
        <p>
            Attributes are serialized DOM attributes. Properties assign values directly to DOM or custom-element properties, so objects and arrays stay
            values instead of becoming strings.
        </p>

        <h2>Named and whole-object attributes</h2>

        <p>
            <a x-attr:title="state.title" x-attr:href="state.href">Hover me and inspect the link</a>
        </p>
        <p x-attr="state.attributes">The attribute object supplies <code>data-demo</code> and <code>aria-label</code>.</p>

        <pre x-pre><code>&lt;a x-attr:title="state.title" x-attr:href="state.href"&gt;Hover me&lt;/a&gt;
&lt;p x-attr="state.attributes"&gt;Attribute spread&lt;/p&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Dynamic attribute names</h2>

        <p x-attr:[state.attributeName]="state.attributeValue">
            The attribute name comes from a restricted expression, not from executed JavaScript.
        </p>

        <pre x-pre><code>&lt;p x-attr:[state.attributeName]="state.attributeValue"&gt;Dynamic name&lt;/p&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Properties and whole-object properties</h2>

        <p>
            The first menu receives its array through the named <code>menu</code> property. The second receives the same public property through a
            whole-object property map.
        </p>

        <x-menu x-prop:menu="state.menu"></x-menu>
        <x-menu x-prop="state.menuProps"></x-menu>

        <pre x-pre><code>&lt;x-menu x-prop:menu="state.menu"&gt;&lt;/x-menu&gt;
&lt;x-menu x-prop="state.menuProps"&gt;&lt;/x-menu&gt;</code></pre>

        <x-divider></x-divider>

        <h2>Source-order precedence</h2>

        <p>
            Property bindings are applied in source order. Here the explicit binding comes after the whole-object expansion, so it overrides the
            object's <code>menu</code> member.
        </p>

        <x-menu x-prop="state.menuProps" x-prop:menu="state.overrideMenu"></x-menu>

        <pre x-pre><code>&lt;x-menu
    x-prop="state.menuProps"
    x-prop:menu="state.overrideMenu"&gt;
&lt;/x-menu&gt;</code></pre>

        <h2>Dynamic property names</h2>

        <p>
            The final example uses the bracketed property form to bind <code>menu</code> by name.
        </p>
        <x-menu x-prop:[state.propertyName]="state.dynamicMenu"></x-menu>
    `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.title = "A bound title";
                state.href = "/pages/07-x-templates/02-bindings.js";
                state.attributes = {"data-demo": "attribute-spread", "aria-label": "Attribute spread example"};
                state.attributeName = "data-dynamic";
                state.attributeValue = "from an expression";
                state.menu = [
                    {label: "Named property", href: "/pages/07-x-templates/02-bindings.js"}
                ];
                state.menuProps = {
                    menu: [
                        {label: "Whole-object property", href: "/pages/07-x-templates/02-bindings.js"}
                    ]
                };
                state.overrideMenu = [
                    {label: "Later binding wins", href: "/pages/07-x-templates/02-bindings.js"}
                ];
                state.propertyName = "menu";
                state.dynamicMenu = [
                    {label: "Dynamic property name", href: "/pages/07-x-templates/02-bindings.js"}
                ];
            }
        };
    }
};
