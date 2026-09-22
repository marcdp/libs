
// contract
export const contract = {
    description: "Displays an avatar with optional identity details and command action.",
    events: {
        command: {
            description: "Raised when the avatar is clicked with a command configured.",
            detail: {
                command: {type:"string"},
                data: {type:"object"}
            }
        }
    },
    properties: {
        initials: {type:"string", default:"", attr:true, state:true, description:""},
        icon:     {type:"string", default:"", attr:true, state:true, description:""},
        image:    {type:"string", default:"", attr:true, state:true, description:""},
        label:    {type:"string", default:"", attr:true, state:true, description:""},
        message:  {type:"string", default:"", attr:true, state:true, description:""},
        command:  {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:flex; flex-direction:row; user-select:none;}
        :host(.cursor) {cursor:pointer;}
        :host div.circle {
            display:flex; 
            box-sizing:border-box;
            aspect-ratio:1/1;
            height:2.25em; 
            border-radius:50%;
            background:var(--x-avatar-background);
            align-items:center;
            justify-content:center;            
        }
        div.circle span {color:var(--x-avatar-text-color); font-weight:600;}
        div.circle + div.details {margin-left:.75em; display:flex; flex-direction:column; justify-content:center;}

        div.details span.label {font-weight:600;}
        div.details span.message {font-size:var(--x-font-size-x-small); }
    `,
    template: `
        <div class="circle">
            <x-icon x-if="state.icon" x-attr:icon="state.icon"></x-icon>
            <img    x-elseif="state.image" x-attr:src="state.image"></x-icon>
            <span   x-elseif="state.initials" x-text="state.initials"></span>
        </div>
        <div class="details" x-if="(state.label || state.message ? true : false)">
            <span class="label"   x-text="state.label"></span>
            <span class="message" x-text="state.message"></span>
        </div>
    `,
    state: {
    },
    script({ state }) {
        return {
            onCommand(command, params){
                if (command == "load") {
                    //load
                    this.addEventListener("click", ()=>{
                        if (state.command) {
                            this.dispatchEvent(new CustomEvent("command", {detail: {command: state.command, data: this.dataset}, bubbles: true, composed: false}));
                        }
                    });
                } 
            }
        }
    }
}
