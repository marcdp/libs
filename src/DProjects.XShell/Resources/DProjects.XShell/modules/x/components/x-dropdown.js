import XElement from "x-element";
import xshell from "xshell";
import { findFocusableElement, getDeepActiveElement, isDescendantOfElement } from "../utils/dom.js";

// class
export default XElement.define("x-dropdown", {
    style: `
        :host {display:inline-block; position:relative;}
        :host .header {cursor:pointer; }
        :host .header.expanded {color:var(--x-color-primary); }

        :host .body {
            display:none;
            position:absolute;
            z-index:10;
            background:var(--x-dropdown-background);
            box-sizing:border-box;
            border:var(--x-dropdown-border);
            border-radius:var(--x-dropdown-border-radius);
            box-shadow:var(--x-dropdown-shadow);
        }
        .body > div {
            padding: var(--x-dropdown-padding-vertical) var(--x-dropdown-padding-horizontal) var(--x-dropdown-padding-vertical) var(--x-dropdown-padding-horizontal);            
            max-height:calc(100vh - 7em);
            scrollbar-width: var(--x-scrollbar-width);
            scrollbar-gutter: var(--x-scrollbar-gutter);
        }
        :host .body.expanded {
            display:block;
            max-width:95vw;
        }

        /* popover */
        :host(.popover) .body {            
            margin-top:1em;
            margin-left:-.5em;
            min-width: clamp(22em, 100%, 200%);
        }
        :host(.popover) .body .helper {
            display: inline-block;
            position:absolute;
            z-index:10;
            left: .75em;
            top:-.9em;
            width: 0;
            height: 0;
            border-left: .9em solid transparent;
            border-right: .9em solid transparent;
            border-bottom: .9em solid var(--x-dropdown-border-color); 
        }                  
        :host(.popover) .body .helper2 {
            display: inline-block;
            left: .75em;
            position:absolute;
            z-index:10;
            top:-.8em;
            width: 0;
            height: 0;
            border-left: .9em solid transparent;
            border-right: .9em solid transparent;
            border-bottom: .9em solid var(--x-dropdown-background); 
        }      

        /* popover left */
        :host(.popover.left) .body {transform:translateX(calc(-100% + 4em));}
        :host(.popover.left) .body .helper {left:unset; right:1em;}
        :host(.popover.left) .body .helper2 {left:unset; right:1em;}
            
        /* input-dropdown */
        :host(.input-dropdown) .header.expanded {
            --x-datafield-border-radius: .5em .5em 0 0;
        }
        :host(.input-dropdown) .body {
            width:100%;
            border-radius:  0 0 var(--x-datafield-border-radius) var(--x-datafield-border-radius);
            border-top:none;
        }
    `,
    template: `
        <div class="header" x-class:expanded="state.expanded" x-on:focusin="focus-head" x-on:mousedown.stop="mousedown-head" x-on:click="click-head" x-on:keydown.enter="click-head">
            <slot></slot>
        </div>
        <div class="body" x-class:expanded="state.expanded" x-on:collapse.stop="collapse" x-on:mousedown.stop="mousedown-body" x-on:click="click-body">
            <span class="helper"></span>
            <span class="helper2"></span>
            <div>
                <slot name="dropdown"></slot>
            </div>
        </div>
    `,
    state:{
        expanded: false, 
        collapseOnClick: false
    },
    methods: {
        onCommand(command) {
            if (command == "load") {
                //load
                this.shadowRoot.addEventListener("focusout", (event) => {
                    if (!this.state.collapseOnClick) {
                        //if new focused element is a descendant of this element, does nothing
                        let relatedTarget = event.relatedTarget;
                        if (isDescendantOfElement(this, relatedTarget)) return;
                        //if last mousedown was less than 10ms ago, does nothing
                        let diff = performance.now() - this._mousedownBodyAt;
                        if (isNaN(diff) || diff > 10) this.onCommand("collapse");
                    }
                });
                this.bindEvent(xshell.bus, "xshell:navigation:start", () => {
                    //if navigation occurred, collapse
                    if (this.state.expanded) {
                        if (!this.state.collapseOnClick) {
                            this.onCommand("collapse");
                        }
                    }
                });
                
            } else if (command == "focus-head") {
                //focus-head
                if (!this.state.collapseOnClick) {
                    this.onCommand("expand");
                }

            } else if (command == "mousedown-head") {
                //mousedown (remember mousedown time)
                this._mousedownHeadAt = performance.now();

            } else if (command == "click-head") {
                //click-head
                if (this.state.collapseOnClick) {
                    if (this.state.expanded) {
                        let diff = performance.now() - this._expandedAt;
                        if (diff > 200) {
                            this.onCommand("collapse");
                        }
                    } else {
                        this.onCommand("expand");    
                    }
                } else {
                    this.onCommand("expand");
                }

            } else if (command == "mousedown-body") {
                //mousedown (remember mousedown time)
                this._mousedownBodyAt = performance.now();

            } else if (command == "click-body") {
                //click-body
                let a = findFocusableElement(this);
                let activeElement = getDeepActiveElement();
                if (a != null && activeElement && activeElement.localName == "body") {
                    //if click in body, focus on first focusable element
                    a.focus();
                }

            } else if (command == "expand") {
                //expand
                if (!this.state.expanded) {
                    this._expandedAt = performance.now();
                    this.state.expanded = true;
                }

            } else if (command == "collapse") {
                //collapse
                if (this.state.expanded) {
                    this.state.expanded = false;
                }
            }
        }
    }
});

