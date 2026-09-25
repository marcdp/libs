// contract
export const contract = {
    description: "Basics components"
};

// export page
export default {
    template: `

        <p>
            Basic visual and interactive components provided by XShell.
        </p>

        <h2>Buttons</h2>

        <div>
            <x-button label="Default button"></x-button>
            <x-button class="submit" label="Submit"></x-button>
            <x-button class="cancel" label="Cancel"></x-button>
            <x-button class="anchor" label="Anchor"></x-button>
        </div>

        <p>
            <x-button icon="x-add" label="Add"></x-button>
            <x-button icon="x-settings" label="Settings"></x-button>
            <x-button icon="x-bell"></x-button>
        </p>

        <x-divider></x-divider>

        <h2>Icons</h2>

        <p>
            <x-icon icon="x-add"></x-icon>
            <x-icon icon="x-settings"></x-icon>
            <x-icon icon="x-bell"></x-icon>
            <x-icon icon="x-close"></x-icon>
        </p>

        <p>
            <x-icon class="size-x2" icon="x-add"></x-icon>
            <x-icon class="size-x2" icon="x-settings"></x-icon>
            <x-icon class="size-x2" icon="x-bell"></x-icon>
        </p>

        <x-divider></x-divider>

        <h2>Badges</h2>

        <p>
            <x-badge value="1"></x-badge>
            <x-badge value="12"></x-badge>
            <x-badge value="999+"></x-badge>
            <x-badge class="plain" value="42"></x-badge>
        </p>

        <x-divider></x-divider>

        <h2>Avatar</h2>

        <x-avatar
            initials="AB"
            label="Example user"
            message="user@example.com">
        </x-avatar>

        <x-divider></x-divider>

        <h2>Notices</h2>

        <x-notice type="info" label="Information" message="This is an informational notice."></x-notice>
        <x-notice type="success" label="Success" message="The operation completed successfully."></x-notice>
        <x-notice type="warning" label="Warning" message="Something requires your attention."></x-notice>
        <x-notice type="error" label="Error" message="The operation could not be completed."></x-notice>
        <x-notice type="working" label="Working" message="Please wait while the operation completes."></x-notice>

        <x-divider></x-divider>

        <h2>Spinner</h2>

        <x-spinner message="Loading..."></x-spinner>

        <x-divider></x-divider>

        <h2>Anchor</h2>

        <x-anchor class="plain" href="/pages/01-components/02-forms.js">
            Open the forms examples
        </x-anchor>

        <x-divider></x-divider>

        <h2>Clock</h2>

        <x-clock></x-clock>

        <x-divider></x-divider>

        <h2>Error</h2>

        <x-error
            code="404"
            message="The requested example could not be found."
            src="/pages/example.js">
        </x-error>

        <x-divider></x-divider>

        <h2>HTML source</h2>

        <x-html value="&lt;p&gt;A &lt;strong&gt;safe inline example&lt;/strong&gt;.&lt;/p&gt;"></x-html>

        <x-divider></x-divider>

        <h2>Internationalized text</h2>

        <x-i18n text="Open the forms examples"></x-i18n>

        <x-divider></x-divider>

        <h2>Loading</h2>

        <x-loading></x-loading>

        <x-divider></x-divider>

        <h2>Markdown</h2>

        <x-markdown value="## Inline Markdown&#10;&#10;This **local** example uses no remote content."></x-markdown>
    `,    
    controller({ }) {
        return {
            load() {
               // load
            }
        };
    }
}
