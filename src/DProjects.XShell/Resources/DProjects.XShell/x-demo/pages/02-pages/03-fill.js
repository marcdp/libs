// contract
export const contract = {
    description: "Query parameters test page",
    events: {},
    properties: {},
    methods: {}
};

// implementation
export default {
    style:`
        x-fill {
            display: flex;
            align-items: center;
            justify-content: center;
            outline: .1em black dotted;
            outline-offset: -1em;
            background: var(--x-color-background-page);
        }
    `,
    template: `
        <x-fill>
            <div>
                This content fills the available container.
            </div>
        </x-fill>
    `,
    controller({ page }) {
        return {
            load() {
            },
            mount() {
                const parentNodeIsBody = page.host.parentNode === document.body;
                if (parentNodeIsBody) {
                    window.scrollTo(0, 0);
                }
            }
        };
    }
};