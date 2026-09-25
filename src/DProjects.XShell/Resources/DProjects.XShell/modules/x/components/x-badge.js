
// contract
export const contract = {
    description: "Displays a short badge value.",
    events: {},
    properties: {
        value: {type:"string", default:"", attribute:true, state:true, description:""}
    },
    methods: {}
};


// definition
export default  {
    style: `
        :host {}
        :host .message {
            display:inline-block;
            border-radius:1em; 
            background-color:var(--x-color-primary); 
            color:white; 
            padding:.225em .5em .175em .5em; 
            font-size:var(--x-font-size-small); 
            font-weight:600; 
            text-align:center;
            min-width:1.75em;
            box-sizing:border-box;
        }

        :host(.plain) .message {
            color: var(--x-color-primary); 
            background-color: white; 
            border:.1em var(--x-color-primary) solid;
        }
    `,
    template: `
        <span class="message" x-text="state.value" ></span>
    `,
    state: {
    }
};


