import XElement from "x-element";

// class
export default XElement.define("x-richtext", {
    style: `
        .editor {
            min-height: 10em;
            padding:.5em;
            outline: none;
            border-top: var(--x-datafield-border);            
        }
        .editor:focus {
            border-color:var(--x-color-black);
        }
    `,
    state: {
        value:"",
        lang:"",
        spellcheck: "true"
    },
    template: `
        <x-toolbar>
            <x-button class="anchor big" x-on:click="action" data-action="bold"          icon="x-format_bold" title="Bold"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="italic"        icon="x-format_italic" title="Italic"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="underline"     icon="x-format_underlined" title="Underline"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="strikeThrough" icon="x-format_strikethrough" title="strikeThrough"></x-button>
            <x-divider></x-divider>
            <x-button class="anchor big" x-on:click="action" data-action="justifyLeft"   icon="x-format_align_left" title="Align left"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="justifyCenter" icon="x-format_align_center" title="Align center"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="justifyright"  icon="x-format_align_right" title="Align right"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="justifyFull"   icon="x-format_align_justify" title="Align justifty"></x-button>
            <x-divider></x-divider>
            <x-button class="anchor big" x-on:click="action" data-action="indent"   icon="x-format_indent_increase" title="Indent"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="outdent"  icon="x-format_indent_decrease" title="Outdent"></x-button>
            <x-divider></x-divider>
            <x-button class="anchor big" x-on:click="action" data-action="insertUnorderedList"  icon="x-format_list_bulleted" title="Insert unordered list"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="insertOrderedList"    icon="x-format_list_numbered" title="Insert ordered list"></x-button>
            <x-divider></x-divider>
            <x-button class="anchor big" x-on:click="action" data-action="removeFormat"             icon="x-format_clear" title="Clear format"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="insertHorizontalRule"     icon="x-remove" title="Insert horizontal rule"></x-button>            
            <x-button class="anchor big" x-on:click="action" data-action="undo"                     icon="x-undo" title="Undo"></x-button>
            <x-button class="anchor big" x-on:click="action" data-action="redo"                     icon="x-redo" title="Redo"></x-button>
            <slot name="toolbar"></slot>
        </x-toolbar>
        <div class="editor" contenteditable="true" x-html="state.value" x-attr:lang="state.lang" x-attr:spellcheck="state.spellcheck"></div>
    `,
    //settings:{
    //    observedAttributes: ["value", "lang", "spellcheck"]
    //},
    methods:{
        onCommand(command, args){
            if (command == "load"){
                //load
                this.addEventListener("focusout", (event) => {
                    this.onCommand("change", {event});
                });

            } else if (command == "action") {
                //action
                let event = args.event;
                let action = event.target.dataset.action;
                document.execCommand(action, false, null);

            } else if (command == "change") {
                //change
                let target = this.shadowRoot.querySelector(".editor");
                let oldValue = this.state.value;
                let newValue = target.innerHTML;
                if (newValue == "<br>") newValue = "";
                this.state.value = newValue;
                this.dispatchEvent(new CustomEvent("change", {detail: {oldValue, newValue}, bubbles: true, composed: false}));
                
            }
        }

    }
});

