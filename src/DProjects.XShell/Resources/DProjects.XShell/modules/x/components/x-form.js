// constants
const inputTypesThatAcceptsEnters = ["text", "password", "number", "date", "datetime-local", "time", "week", "month", "url", "email", "phone", "search"];

// contract
export const contract = {
    description: "Coordinates data-field validation, submission, loading state, and wizard navigation.",
    events: {
        command: {
            description: "Raised when the form submits successfully.",
            detail: {
                command: {type:"string"},
                data: {type:"object"}
            }
        }
    },
    properties: {
        wizard:         {type:"boolean", default:false, attribute:true, state:true, description:""},
        wizardDirection:{type:"string", default:"", attribute:true, state:true, description:""},
        wizardIndex:    {type:"number", default:0, attribute:true, state:true, description:""},
        wizardPanels:   {type:"array", default:[], attribute:true, state:true, description:""},
        validated:      {type:"boolean", default:false, attribute:true, state:true, description:""},
        command:        {type:"string", default:"submit", attribute:true, state:true, description:""},
        errors:         {type:"array", default:[], attribute:true, state:true, description:""},
        loading:        {type:"boolean", default:false, attribute:true, state:true, description:""},
        loadingLabel:   {type:"string", default:"Working", attribute:true, state:true, description:""},
        loadingMessage: {type:"string", default:"Please wait...", attribute:true, state:true, description:""},
    },
    methods: {
        validate: {
            description: "Validates the form's data fields.",
            parameters: [],
            returns: {
                description: "The validation errors found in the form.",
                type: "array"
            }
        },
        showLoading: {
            description: "Shows the loading notice and optionally updates its text.",
            parameters: [
                {name:"options", type:"object", description:"Optional label and message values for the loading notice."}
            ],
            returns: {
                description: "Does not return a value.",
                type: "void"
            }
        },
        hideLoading: {
            description: "Hides the loading notice.",
            parameters: [],
            returns: {
                description: "Does not return a value.",
                type: "void"
            }
        }
    }, 
    slots: {
        "": {
            description: "The default slot for form content."
        },
        "header": {
            description: "The slot for the form header content."
        },
        "cancel":{
            description: "The slot for the form cancel button content."
        },
        "footer": {
            description: "The slot for the form footer content."
        }
    }
};


// implementation
export default {
    style: `
        :host {display:block;}

        .header {}

        :host .header ::slotted(h2) {margin-top:0;}

        .container[vertical] {display:flex;}
        .container[vertical] > x-wizard-header {flex:0; margin-right:2em;}
        .container[vertical] > div {flex:1; width:100%;}
        :host([wizard]) .body ::slotted(*) {display:none;}
                
        .wizard-header {display:flex; margin:0; padding:0; justify-content:center}
        .wizard-header li {list-style:none; display:block; align-items:center; width:10em; text-align:center; padding:.5em; position:relative;}
        .wizard-header li > span {display:block; z-index:1; position:relative}
        .wizard-header li > span.icon {width:1.6em; height:1.6em; text-align:center; border:.1em var(--x-color-primary) solid; color:var(--x-color-primary); margin:0 auto; border-radius:50%; line-height:1.5em;}
        .wizard-header li > span.icon .number {font-size:var(--x-font-size-small);}
        .wizard-header li > span.label {margin-top:.25em;}
        .wizard-header li > span.message {font-size:var(--x-font-size-small); }
        .wizard-header li:before {content:"";border:.075em gray solid; top:1.25em;width:calc(50% - 1.25em);left:0;position:absolute;}
        .wizard-header li:after {content:"";border:.075em gray solid; top:1.25em;width:calc(50% - 1.25em);right:0;position:absolute;}
        .wizard-header li:first-child:before {display:none;}
        .wizard-header li:last-child:after {display:none;}
        .wizard-header li[visited] span.label {}
        .wizard-header li[visited] span.icon {background:var(--x-color-primary); color:white;}
        .wizard-header li[selected] span.label {font-weight:600; }

        .errors {margin-top:1em;padding:1em;color:var(--x-datafield-error-color);border: var(--x-datafield-border); border-color:var(--x-datafield-error-color);border-radius:var(--x-datafield-border-radius);}
        .errors div {font-size: var(--x-font-size-small);}
        .errors ul {list-style-position:outside; margin:0; padding:0; padding-left:1.5em; margin-top:1em;}
        .errors ul li {}
 
        .footer-separator {
            display:var(--x-form-footer-hr-display, none);
            margin-left:var(--x-form-footer-hr-margin-left);
            margin-right:var(--x-form-footer-hr-margin-right);
            transform:translateY(1em);
            border-bottom: var(--x-form-border);
        }

        .footer {display:flex; align-items:end; margin-top:1em; }
        .footer x-button {min-width: var(--x-button-width-wide); }
        .footer > div {display:flex; justify-content:flex-end; gap:.25em; margin-left:.25em; padding-top:1em; flex:1;}
        .footer ::slotted(x-button) {min-width: var(--x-button-width-wide);}

        @media only screen and (max-width: 768px) {
            .container[vertical] {display:block;}
            .container[vertical] > x-wizard-header {margin-right:0; margin-bottom:1em;}
        }

    `,
    template: `
        <div class="header">
            <slot name="header"></slot>
        </div>

        <div x-if="state.loading" class="loading">
            <x-notice type="working" x-attr:label="state.loadingLabel" x-attr:message="state.loadingMessage" ></x-notice>
        </div>
        <div x-else class="container" x-attr:vertical="state.wizardDirection == 'vertical' ? true : false">

            <x-wizard-header x-if="state.wizard" x-attr:index="state.wizardIndex" x-attr:class="state.wizardDirection" x-on:index-set="wizardSet">
                <div x-for="wizardPanel in state.wizardPanels" 
                    x-attr:label="wizardPanel.label"
                    x-attr:message="wizardPanel.message"
                    x-attr:icon="wizardPanel.icon"
                ></div>
            </x-wizard-header>

            <div>
                <div class="body">
                    <slot x-on:slotchange="refresh"></slot>
                </div>

                <div class="errors" x-attr:hidden="state.errors.length == 0">
                    <div>
                        Solve error before continue:
                        <ul>
                            <li x-for="error in state.errors">
                                <span x-if="error.path">
                                    [<span x-html="error.path"></span>]
                                </span>
                                <b><span x-html="error.label"></span>:</b>
                                <span x-html="error.message"></span>
                            </li>
                        </ul>
                    </div>
                </div>

                <div class="footer-separator"></div>
                
                <div class="footer">
                    <slot name="cancel"></slot>
                    <div x-if="state.wizard">
                        <x-button x-if="state.wizardIndex > 0" label="Prev" x-on:click="wizardPrev"></x-button>
                        <x-button x-if="state.wizardIndex < state.wizardPanels.length - 1" label="Next" x-on:click="wizardNext"></x-button>
                        <slot x-else name="footer"></slot>
                    </div>
                    <div x-else>
                        <slot name="footer"></slot>
                    </div>
                </div>
            </div>
            
        </div>

    `,
    state: {
    },
    controller({ state }) {
        let styleSheet = new CSSStyleSheet();
        return {

            load(args) {
                //load
                this.shadowRoot.addEventListener("command", (event) => {
                    if (event.detail.command == "submit") {
                        this.onCommand(event.detail.command);
                        event.stopPropagation();
                    }
                });
                this.shadowRoot.addEventListener("datafield:change", () => {
                    if (state.validated) {
                        this.onCommand("validate");
                    }
                });
                this.shadowRoot.addEventListener("keypress", (event) => {
                    if (event.keyCode == 13) {
                        var type = event.target.type;
                        if (inputTypesThatAcceptsEnters.indexOf(type) != -1) {
                            event.stopPropagation();
                            setTimeout(()=>{this.onCommand("submit");}, 0);
                        }
                    }
                });
                this.onCommand("refresh");
            },
            mount() {
                this.shadowRoot.adoptedStyleSheets = [...this.shadowRoot.adoptedStyleSheets, styleSheet];
            },
            showLoading({label, message}) {
                state.loading = true;
                if (label) state.loadingLabel = label;
                if (message) state.loadingMessage = message;
            },
            hideLoading() {
                state.loading = false;
            },
            wizardSet(args) {
                //wizardSet
                let index = args.event.detail.index;
                state.wizardIndex = index;
                this.onCommand("refresh");
            },
            wizardPrev(args) {
                //wizardPrev
                state.errors = [];
                state.wizardIndex--;
                this.onCommand("refresh");
            },
            wizardNext(args) {
                //wizardNext
                //get current wizard panel
                let wizardPanel = state.wizardPanels[state.wizardIndex];
                let wizardPanelElement = this.querySelector(`:scope > *:nth-child(${ wizardPanel.index })`);
                //validate errors in current wizard panel
                let errors = [];
                wizardPanelElement.querySelectorAll("x-datafield").forEach((element) => {
                    for(let error of element.validate(true)) {
                        errors.push(error);
                    }
                });
                //show errors
                state.errors = errors;
                state.validated = true;
                //if not error, advance to next panel
                if (state.errors.length == 0) {
                    state.wizardIndex++;
                    this.onCommand("refresh");
                }
            },
            validate(args) {
                //validate
                let errors = [];
                if (state.wizard) {
                    let wizardPanel = state.wizardPanels[state.wizardIndex];
                    let wizardPanelElement = this.querySelector(`:scope > *:nth-child(${ wizardPanel.index })`);
                    wizardPanelElement.querySelectorAll("x-datafield").forEach((element) => {
                        for(let error of element.validate(true)) {
                            errors.push(error);
                        }                    
                    });
                } else {
                    this.querySelectorAll("x-datafield").forEach((element) => {
                        for(let error of element.validate(true)) {
                            errors.push(error);
                        }
                    });
                }
                state.errors = errors;
                state.validated = true;
                return state.errors;
            },
            submit(args) {
                //submit
                this.onCommand("validate");
                if (state.errors.length == 0) {
                    this.dispatchEvent(new CustomEvent("command", {detail: {command: state.command, data: this.dataset}, bubbles: true, composed: false}));
                }
            },
            refresh(args) {
                //refresh
                if (state.wizard) {
                    let wizardPanels = [];
                    this.querySelectorAll(":scope > *[label]").forEach((panel) => {
                        if (!panel.hasAttribute("slot")) {
                            wizardPanels.push({
                                label: panel.getAttribute("label"),
                                message: panel.getAttribute("message"),
                                icon: panel.getAttribute("icon") || "",
                                index: Array.from(panel.parentNode.children).indexOf(panel) + 1
                            });
                        }
                    });
                    state.wizardPanels = wizardPanels;
                    styleSheet?.replaceSync(`
                        :host([wizard]) .body ::slotted(*:nth-child(${ wizardPanels[state.wizardIndex].index })) {
                            display: block;
                        }
                    `);
                }
            }
        };
    }
};

