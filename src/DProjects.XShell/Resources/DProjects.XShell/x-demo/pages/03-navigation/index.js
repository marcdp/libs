// contract
export const contract = {
    description: "Navigation overview",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Navigation</h2>

        <p>
            XShell Navigation maps a destination to a Page. The same Page and navigation APIs work in path mode and hash mode; Navigation generates
            the browser URL for the configured mode.
        </p>

        <h3>Examples</h3>

        <ul>
            <li>
                <x-anchor href="01-basic.js">Basic</x-anchor>
                replaces or navigates the current top-level destination.
            </li>
            <li>
                <x-anchor href="02-areas.js">Areas</x-anchor>
                shows how a navigation context adds its prefix and composes module menus.
            </li>
            <li>
                <x-anchor href="03-stack.js">Stack</x-anchor>
                opens multiple Pages represented in Navigation's Page stack.
            </li>
            <li>
                <x-anchor href="04-embedded.js">Embedded</x-anchor>
                renders a Page into a local named outlet instead of the main navigation stack.
            </li>
        </ul>

        <p>
            These examples use canonical Page destinations or friendly menu paths and leave browser URL generation to Navigation.
        </p>
    `
};
