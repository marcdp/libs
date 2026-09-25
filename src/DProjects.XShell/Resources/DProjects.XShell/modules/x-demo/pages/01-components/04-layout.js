// contract
export const contract = {
    description: "Layout page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <h1>Layout components</h1>

        <p>
            Components for grouping content, switching views, collapsing sections,
            arranging actions, and progressively loading UI.
        </p>

        <h2>Card</h2>

        <x-card>

            <h3 slot="header">Card header</h3>

            <p>
                This is the main card content.
            </p>

            <p>
                Cards provide separate header, body, and footer areas.
            </p>

            <p slot="footer">
                Card footer
            </p>

        </x-card>

        <x-divider></x-divider>

        <h2>Tabs</h2>

        <x-tabs>

            <x-tab
                label="Overview"
                hash="overview">

                <h3>Overview</h3>

                <p>
                    Content for the overview tab.
                </p>

            </x-tab>

            <x-tab
                label="Details"
                hash="details">

                <h3>Details</h3>

                <p>
                    Content for the details tab.
                </p>

            </x-tab>

            <x-tab
                label="Settings"
                hash="settings">

                <h3>Settings</h3>

                <p>
                    Content for the settings tab.
                </p>

            </x-tab>

        </x-tabs>

        <x-divider></x-divider>

        <h2>Accordion</h2>

        <x-accordion>

            <x-accordion-panel
                label="First section"
                icon="x-folder"
                expanded>

                <p>
                    Content inside the first accordion panel.
                </p>

            </x-accordion-panel>

            <x-accordion-panel
                label="Second section"
                icon="x-file">

                <p>
                    Content inside the second accordion panel.
                </p>

            </x-accordion-panel>

            <x-accordion-panel
                label="Third section">

                <p>
                    Content inside the third accordion panel.
                </p>

            </x-accordion-panel>

        </x-accordion>

        <x-divider></x-divider>

        <h2>Toolbar</h2>

        <x-toolbar>

            <x-button
                class="anchor"
                icon="x-add"
                label="Add">
            </x-button>

            <x-button
                class="anchor"
                icon="x-edit"
                label="Edit">
            </x-button>

            <x-button
                class="anchor"
                icon="x-delete"
                label="Delete">
            </x-button>

            <x-divider></x-divider>

            <x-button
                class="anchor"
                icon="x-settings"
                label="Settings">
            </x-button>

        </x-toolbar>

        <x-divider></x-divider>

        <h2>Dropdown</h2>

        <p>
            Basic dropdown:
        </p>

        <x-dropdown>

            <x-button
                label="Open dropdown"
                icon="x-keyboard-arrow-down">
            </x-button>

            <div slot="dropdown">
                <p>Dropdown content</p>
                <p>
                    This area may contain arbitrary components or HTML.
                </p>
            </div>

        </x-dropdown>

        <p>
            Popover:
        </p>

        <x-dropdown class="popover">

            <x-button
                label="Open popover"
                icon="x-info">
            </x-button>

            <div slot="dropdown">
                <strong>Popover content</strong>
                <p>
                    Additional information can be displayed here.
                </p>
            </div>

        </x-dropdown>

        <x-divider></x-divider>

        <h2>Lazy content</h2>

        <p>
            The content below is activated when the lazy container approaches
            the visible viewport.
        </p>

        <x-lazy>

            <x-card>
                <h3>Lazy-loaded card</h3>

                <p>
                    This component is inside an x-lazy container.
                </p>
            </x-card>

        </x-lazy>

        <x-divider></x-divider>

        <h2>Fill</h2>

        <p>
            x-fill is intended for layouts where content must occupy the complete
            available positioned container.
        </p>

        <div style="position:relative; height:10em; border:1px solid currentColor;">

            <x-fill>
                <div>
                    This content fills the available container.
                </div>
            </x-fill>

        </div>
    `,  
    controller({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
