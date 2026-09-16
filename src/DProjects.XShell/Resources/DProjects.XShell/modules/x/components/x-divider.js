
// class
export default {
    style: `
        :host {display:block;}
        :host hr {border:var(--x-divider-border); margin-left:0; margin-right:0;}

        :host-context(x-menu) hr {margin-top:.3em; margin-bottom:.3em; margin-left:-.25em; margin-right:-.25em; border-top:none; }
        :host-context(x-menu.plain) hr {margin-left:.4em; margin-right:.4em;}
        :host-context(x-toolbar) hr {height:2em; margin:0; display:block}
    `,
    template: `<hr/>`
};

