
// export
export default {
    meta: {
        renderEngine: "x",
        stateEngine:  "proxy"
    },
    style: `
        :host {}
        :host a {display:inline; align-items:center; width:100%; }
        :host a[disabled] { pointer-events: none; color:gray;}
        
        :host(.menuitem) {}
        :host(.menuitem) a {display:flex; padding-left:.6em; padding-right:.6em; text-decoration:none;}

        :host(.plain) {}
        :host(.plain) a {text-decoration:none; color:var(--x-color-text)}
        :host(.plain) a:hover {color:var(--x-color-primary);}
        :host(.plain) a:active {color:var(--x-color-primary-dark)}
        :host(.plain.selected) a {color:var(--x-color-primary);}
        :host(.plain.selected) a:hover {color:var(--x-color-primary-dark);}    
    `,
    template: `
        <a part="a" x-attr:href="state.hrefReal" x-attr:disabled="state.disabled" x-attr:target="state.target" x-attr:rel="state.rel" x-on:click="click"><slot></slot></a>
    `,
    state: {
        href:       {value: "",     attr:true, prop:true},
        open:       {value: "auto", attr:true, enum: ["auto","top","dialog","stack","embed"]},
        qs:         {value: {},     attr:true},
        breadcrumb: {value: false,  attr:true},
        title:      {value: null,   attr:true},
        icon:       {value: null,   attr:true},
        disabled:   {value: false,  attr:true},
        target:     {value: null,   attr:true},
        outlet:     {value: null,   attr:true},
        rel:        {value: null,   attr:true, reflect:true},
        replace:    {value: false,  attr:true},
        hrefReal:   {value: null}
    },
    script({ state, navigation, getPage }) {
        return {
            onCommand(command, params){
                if (command == "load") {
                    // load

                } else if (command == "stateChange") {
                    // refresh
                    if (state.href) {
                        const page = getPage();
                        const href = navigation.buildUrlAbsolute({
                            href:       state.href,
                            params:     state.qs,
                            open:       state.open,
                            replace:    state.replace,
                            page:       page,
                            nav: {
                                breadcrumb: (state.breadcrumb ? page?.breadcrumb : null),
                                title:      state.title,
                                icon:       state.icon
                            }
                        });
                        // set real href    
                        state.hrefReal = href;
                    }

                } else if (command == "click") {
                    // click
                    const event = params.event;
                    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || state.target) return;
                    const page = getPage();
                    navigation.navigate( {
                        href: state.href, 
                        params: state.qs,
                        open: state.open,
                        replace: state.replace,
                        page: page,
                        outlet: state.outlet,
                        nav: {
                            breadcrumb: (state.breadcrumb ? page.breadcrumb : null),
                            title:      state.title,
                            icon:       state.icon
                        }
                    });
                    event.preventDefault();
                }
            }
        }
    }
};

