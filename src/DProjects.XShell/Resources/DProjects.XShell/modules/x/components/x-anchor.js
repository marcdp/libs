
// contract
export const contract = {
    description: "Renders a navigation link handled by the XShell navigation service.",
    events: {},
    properties: {
        href:       {type:"string", default:"", attr:true, state:true, description:""},
        open:       {type:"string", default:"auto", attr:true, state:true, description:"", enum: ["auto","top","dialog","stack","embed"]},
        qs:         {type:"object", default:{}, attr:true, state:true, description:""},
        breadcrumb: {type:"boolean", default:false, attr:true, state:true, description:""},
        title:      {type:"string", default:null, attr:true, state:true, description:""},
        icon:       {type:"string", default:null, attr:true, state:true, description:""},
        disabled:   {type:"boolean", default:false, attr:true, state:true, description:""},
        target:     {type:"string", default:null, attr:true, state:true, description:""},
        outlet:     {type:"string", default:null, attr:true, state:true, description:""},
        rel:        {type:"string", default:null, attr:true, state:true, description:"", reflect:true},
        replace:    {type:"boolean", default:false, attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
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
        hrefReal: null
    },
    script({ state, navigation, getPage }) {
        return {
            load(params) {
                // load
            },

            stateChange(params) {
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
            },

            click(params) {
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
};

