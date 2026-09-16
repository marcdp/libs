import XElement from "x-element";

// class
export default {
    style: `
        :host {
            display:block; 
            border:var(--x-card-border); 
            border-radius:var(--x-card-border-radius); 
            box-shadow:var(--x-card-shadow); 
            margin-top:.5em;
            margin-bottom:.5em;
            
        }
        :host .header {}
        :host .header ::slotted(p) {margin:0}

        :host .body {padding:var(--x-card-padding);}
        :host .body ::slotted(h1:first-child) {margin-top:0}
        :host .body ::slotted(h2:first-child) {margin-top:0}
        :host .body ::slotted(h3:first-child) {margin-top:0}
        :host .body ::slotted(h4:first-child) {margin-top:0}
        :host .body ::slotted(h5:first-child) {margin-top:0}

        :host .footer {}
        :host .footer ::slotted(p) {margin:0}
    `,
    template: `
        <div class="header"><slot name="header"></slot></div>
        <div class="body"><slot></slot></div>
        <div class="footer"><slot name="footer"></slot></div>
    `
};

