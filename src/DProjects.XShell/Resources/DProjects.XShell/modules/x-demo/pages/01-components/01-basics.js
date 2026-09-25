// contract
export const contract = {
    description: "Basics page"
};

// export page
export default {
    template: `
        <h1>Basic components</h1>

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

        <x-notice
            type="info"
            label="Information"
            message="This is an informational notice.">
        </x-notice>

        <x-notice
            type="success"
            label="Success"
            message="The operation completed successfully.">
        </x-notice>

        <x-notice
            type="warning"
            label="Warning"
            message="Something requires your attention.">
        </x-notice>

        <x-notice
            type="error"
            label="Error"
            message="The operation could not be completed.">
        </x-notice>

        <x-notice
            type="working"
            label="Working"
            message="Please wait while the operation completes.">
        </x-notice>

        <x-divider></x-divider>

        <h2>Spinner</h2>

        <x-spinner message="Loading..."></x-spinner>
    `,    
    controller({ }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
