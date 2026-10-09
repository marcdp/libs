// contract
export const contract = {
    description: "Renders menu data as menu items.",
    events: {},
    properties: {
        menu: {type:"array", default:null, attribute:false, state:true, description:""},
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display: block;}
        :host(.horizontal) {display:flex; align-items:center; }
        :host(.horizontal) nav {display:flex; align-items:center;}
        :host(.horizontal) nav > x-menuitem {margin-right: 1em;}
    `,
    template: `
        <nav x-if="state.menu" x-attr:class="state.horizontal ? 'horizontal' : ''">
            <x-menuitem x-recursive="menuitem in state.menu" 
                x-key="href" 
                x-attr:embedded="menuitem.embedded" 
                x-attr:label="menuitem.label" 
                x-attr:href="menuitem.path || menuitem.href" 
                x-attr:icon="menuitem.icon" 
                x-attr:selected="menuitem.selected" 
                x-attr:class="menuitem.class"
                x-attr:tooltip="menuitem.tooltip" 
                >
                
            </x-menuitem>
        </nav>        
    `
};

