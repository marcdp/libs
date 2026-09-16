// export page
export default {
    template: `
    
        <x-form command="logout">
            <x-datafields>
                <x-datafield label="ID"     type="text" x-model="state.id" readonly></x-datafield>
                <x-datafield label="Name"   type="text" x-model="state.name" readonly></x-datafield>
                <x-datafield label="Roles"  type="text" x-model="state.roles.join(', ')" readonly></x-datafield>
                <x-datafield label="Claims" type="list" readonly>
                    <table>
                        <tr x-for="item in state.claims" x-key="id">
                            <td>{{ item.key }}:</td>
                            <td>{{ item.value }}</td>
                        </tr>
                    </table>
                </x-datafield>
            </x-datafields>            
            <x-button slot="footer" command="logout" label="Logout" class="submit"></x-button>                
        </x-form>
    `,    
    state: {
        id:     {value:""},
        name:   {value:""},
        roles:  {value:[]},
        claims: {value:[]}
    },
    script({ state, identity, dialog, auth }) {
        return {
            async onCommand(command, params) {
                if (command == "load") {
                    // load
                    state.id = identity.id;
                    state.name = identity.name;
                    state.roles = identity.roles;
                    state.claims = Object.entries(identity.claims).map(([key, value]) => ({ key: key, value: value }));
                    
                } else if (command == "logout") {
                    // logout
                    const result = await dialog.confirm({ 
                        title: "Logout", 
                        message: "Are you sure you want to logout?" 
                    });
                    if (result === "yes") {
                        auth.logout();
                    }
                }                
            }
        };
    }
}
