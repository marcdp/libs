// contract
export const contract = {
    description: "Forms page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <h1>Forms</h1>

        <p>
            Form controls, validation-oriented fields, grouped data fields, and rich text editing.
        </p>

        <h2>Basic fields</h2>

        <x-form>
            <x-datafields columns="2">

                <x-datafield
                    type="text"
                    label="Name"
                    placeholder="Enter your name">
                </x-datafield>

                <x-datafield
                    type="email"
                    label="Email"
                    placeholder="name@example.com">
                </x-datafield>

                <x-datafield
                    type="number"
                    label="Age"
                    min="0"
                    max="120">
                </x-datafield>

                <x-datafield
                    type="date"
                    label="Date">
                </x-datafield>

                <x-datafield
                    type="url"
                    label="Website"
                    placeholder="https://example.com">
                </x-datafield>

                <x-datafield
                    type="tel"
                    label="Phone"
                    placeholder="+34 600 000 000">
                </x-datafield>

            </x-datafields>
        </x-form>

        <x-divider></x-divider>

        <h2>Field states</h2>

        <x-datafields columns="2">

            <x-datafield
                type="text"
                label="Required"
                placeholder="Required value"
                required>
            </x-datafield>

            <x-datafield
                type="text"
                label="Readonly"
                value="Readonly value"
                readonly>
            </x-datafield>

            <x-datafield
                type="text"
                label="Disabled"
                value="Disabled value"
                disabled>
            </x-datafield>

            <x-datafield
                type="text"
                label="With description"
                description="Additional information about this field."
                placeholder="Value">
            </x-datafield>

        </x-datafields>

        <x-divider></x-divider>

        <h2>Text areas</h2>

        <x-datafields columns="2">

            <x-datafield
                type="textarea"
                label="Description"
                placeholder="Write a description">
            </x-datafield>

            <x-datafield
                type="textarea"
                label="Limited text"
                maxlength="200"
                placeholder="Maximum 200 characters">
            </x-datafield>

        </x-datafields>

        <x-divider></x-divider>

        <h2>Boolean fields</h2>

        <x-datafields columns="2">

            <x-datafield
                type="checkbox"
                label="Enabled">
            </x-datafield>

            <x-datafield
                type="checkbox"
                label="Accept terms"
                required>
            </x-datafield>

        </x-datafields>

        <x-divider></x-divider>

        <h2>Grouped fields</h2>

        <x-datafields
            label="Contact information"
            message="Example of several related fields grouped together."
            columns="3">

            <x-datafield
                type="text"
                label="First name">
            </x-datafield>

            <x-datafield
                type="text"
                label="Last name">
            </x-datafield>

            <x-datafield
                type="email"
                label="Email">
            </x-datafield>

            <x-datafield
                type="text"
                label="Address"
                columns="3">
            </x-datafield>

        </x-datafields>

        <x-divider></x-divider>

        <h2>Rich text</h2>

        <x-datafield
            type="richtext"
            label="Content"
            value="<p>Edit this <b>rich text</b> content.</p>">
        </x-datafield>

        <x-divider></x-divider>

        <h2>Standalone rich-text editor</h2>

        <x-richtext class="standalone" value="<p>This is an editable <b>rich text</b> example.</p>">
        </x-richtext>

        <x-divider></x-divider>

        <h2>Form footer</h2>

        <x-form>
            <x-datafields columns="2">

                <x-datafield
                    type="text"
                    label="Username"
                    required>
                </x-datafield>

                <x-datafield
                    type="password"
                    label="Password"
                    required>
                </x-datafield>

            </x-datafields>

            <x-button
                slot="footer"
                class="submit"
                label="Save">
            </x-button>

            <x-button
                slot="cancel"
                class="cancel"
                label="Cancel">
            </x-button>
        </x-form>



        <h2>Wizard form</h2>

        <x-form wizard wizard-direction="horizontal">

            <div
                label="Account"
                message="Basic account information"
                icon="x-person">

                <x-datafields columns="2">

                    <x-datafield
                        type="text"
                        label="Username"
                        required>
                    </x-datafield>

                    <x-datafield
                        type="email"
                        label="Email"
                        required>
                    </x-datafield>

                </x-datafields>

            </div>

            <div
                label="Profile"
                message="Personal information"
                icon="x-edit">

                <x-datafields columns="2">

                    <x-datafield
                        type="text"
                        label="First name"
                        required>
                    </x-datafield>

                    <x-datafield
                        type="text"
                        label="Last name"
                        required>
                    </x-datafield>

                    <x-datafield
                        type="textarea"
                        label="Description"
                        columns="2"
                        placeholder="Tell us something about yourself">
                    </x-datafield>

                </x-datafields>

            </div>

            <div
                label="Preferences"
                message="Application preferences"
                icon="x-settings">

                <x-datafields columns="2">

                    <x-datafield
                        type="checkbox"
                        label="Enable notifications">
                    </x-datafield>

                    <x-datafield
                        type="checkbox"
                        label="Receive email updates">
                    </x-datafield>

                </x-datafields>

            </div>

            <x-button
                slot="cancel"
                class="cancel"
                label="Cancel">
            </x-button>

            <x-button
                slot="footer"
                class="submit"
                command="submit"
                label="Finish">
            </x-button>

        </x-form>


    `,    
    controller({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
