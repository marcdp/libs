import XElement from "x-element";

// constants
const inputTypesThatAcceptsEnters = ["text", "password", "number", "date", "datetime-local", "time", "week", "month", "url", "email", "phone", "search"];

// class
export default XElement.define("x-form", {
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
        .footer x-button {min-width: var(--x-button-width-wide); aflex:1; }
        .footer > div {display:flex; justify-content:flex-end; gap:.25em; margin-left:.25em; padding-top:1em; flex:1;}
        .footer ::slotted(x-button) {min-width: var(--x-button-width-wide);}

        @media only screen and (max-width: 768px) {
            .container[vertical] {display:block;}
            .container[vertical] > x-wizard-header {margin-right:0; margin-bottom:1em;}
        }

    `,
    template: `
        <style x-if="state.wizardStyle" x-html="state.wizardStyle"></style>
        
        <div class="header">
            <slot name="header"></slot>
        </div>

        <div x-if="state.loading" class="loading">
            <x-notice type="working" x-attr:label="state.loadingLabel" x-attr:message="state.loadingMessage" ></x-notice>
        </div>
        <div x-else class="container" x-attr:vertical="state.wizardDirection == 'vertical' ? true : false">

            <x-wizard-header x-if="state.wizard" x-attr:index="state.wizardIndex" x-attr:class="state.wizardDirection" x-on:index-set="wizard-set">
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
                        <x-button x-if="state.wizardIndex > 0" label="Prev" x-on:click="wizard-prev"></x-button>
                        <x-button x-if="state.wizardIndex < state.wizardPanels.length - 1" label="Next" x-on:click="wizard-next"></x-button>
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
        wizard: false,
        wizardDirection: "",
        wizardIndex: 0, 
        wizardPanels: [],
        validated: false,
        command: "submit",
        errors: [],
        loading: false,
        loadingLabel: "Working",
        loadingMessage: "Please wait...",
        wizardStyle: ""
    },
    methods: {
        validate() {
            this.onCommand("validate");
            return this.state.errors;
        },
        showLoading({label, message}) {
            this.state.loading = true;
            if (label) this.state.loadingLabel = label;
            if (message) this.state.loadingMessage = message;
        },
        hideLoading() {
            this.state.loading = false;
        },
        onCommand(command, args) {
            if (command === "load") {
                //load
                this.shadowRoot.addEventListener("command", (event) => {
                    if (event.detail.command == "submit") {
                        this.onCommand(event.detail.command);
                        event.stopPropagation();
                    }
                });
                this.shadowRoot.addEventListener("datafield:change", () => {
                    if (this.state.validated) {
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

            } else if (command == "wizard-set") {
                //wizard-set
                let index = args.event.detail.index;
                this.state.wizardIndex = index;
                this.onCommand("refresh");
                
            } else if (command == "wizard-prev") {
                //wizard-prev
                this.state.errors = [];
                this.state.wizardIndex--;
                this.onCommand("refresh");

            } else if (command == "wizard-next") {
                //wizard-next
                //get current wizard panel
                let wizardPanel = this.state.wizardPanels[this.state.wizardIndex];
                let wizardPanelElement = this.querySelector(`:scope > *:nth-child(${ wizardPanel.index })`);
                //validate errors in current wizard panel
                let errors = [];
                wizardPanelElement.querySelectorAll("x-datafield").forEach((element) => {
                    for(let error of element.validate(true)) {
                        errors.push(error);
                    }                    
                });
                //show errors
                this.state.errors = errors;
                this.state.validated = true;
                //if not error, advance to next panel
                if (this.state.errors.length == 0) {
                    this.state.wizardIndex++;
                    this.onCommand("refresh");
                }

            } else if (command === "validate") {
                //validate
                let errors = [];
                if (this.state.wizard) {
                    let wizardPanel = this.state.wizardPanels[this.state.wizardIndex];
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
                this.state.errors = errors;
                this.state.validated = true;

            } else if (command === "submit") {
                //submit
                this.onCommand("validate");
                if (this.errors.length == 0) {
                    this.dispatchEvent(new CustomEvent("command", {detail: {command: this.state.command, data: this.dataset}, bubbles: true, composed: false}));
                }

            } else if (command == "refresh") {
                //refresh
                if (this.state.wizard) {
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
                    this.state.wizardPanels = wizardPanels;
                    this.state.wizardStyle = `:host([wizard]) .body ::slotted(*:nth-child(${ wizardPanels[this.state.wizardIndex].index })) {display:block;}`;
                }
            }

        }
    }
});

