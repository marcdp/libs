
// export
export default {
    style: `
        :host {display:block; border:1px red solid;}
        ul {margin:0; padding:0;}
        li {list-style:none; }
        li > x-anchor {display:block; margin-bottom:.7em;}
        li > li > li  {padding-left:1.75em;}
        ul > li > ul > li > ul {padding-left:1.75em;}
        hr {border-top:var(--x-layout-main-border); margin-top:1.5em; margin-bottom:1.5em; display:block;}

        :host > nav > ul > li + li {margin-top:1.5em; }
        :host > nav > ul > li > x-anchor {font-size:var(--x-font-size-subtitle); font-weight:bold!important; display:block; margin-bottom:.5em; }

        x-icon.new {padding-left:.15em; display:inline-block; transform: translateY(-.1em);}

        x-anchor.selected {font-weight:600; }     

        :host(.horizontal) ul {display:flex; align-items:center;}
        :host(.horizontal) li {display:flex; align-items:center;}
        :host(.horizontal) li > x-anchor {margin-bottom:0; padding-left:.5em; padding-right:.5em;}
        :host(.horizontal) nav > ul > li > x-anchor {margin:0!important; font-weight:normal!important; font-size:var(--x-font-size); }
    `,
    template: `
        <nav>
            <ul x-if="state.menu">
                <li class="menuitem new" x-recursive="menuitem in state.menu" x-key="href" x-recursive-wrapper="ul">
                    <hr x-if="menuitem.label=='-'" />
                    <x-anchor x-else x-attr:href="menuitem.href" x-attr:target="menuitem.target" x-attr:icon="menuitem.icon" class="plain" x-class:selected="state.selected == menuitem.href" >
                        <x-icon x-if="menuitem.icon" x-attr:icon="menuitem.icon"></x-icon>
                        <span x-text="menuitem.label"></span>
                        <x-icon x-if="menuitem.target && !menuitem.target.startsWith('#')" class="new" icon="x-open_in_new"></x-icon>
                    </x-anchor>
                    <span></span>
                    <span></span>
                    <span></span>
                    <span></span>
                </li>
            </ul>
        </nav>
    `,
    state: {
        src:   {value: ""},
        menu: {value: null}
    },
    script({ events, bus, state, getPage, menus }) {
        return {
            onCommand(command, ...args) {
                if (command == "load") {
                    // load
                    events.on(bus, "xshell:page:load", (event)=>{
                        if (event.detail.id == getPage()?.id) {
                            this.onCommand("refresh");
                        }
                    });

                } else if (command == "mount") {
                    // mount
                    this.onCommand("refresh");

                } else if (command == "refresh") {
                    //refresh
                    state.menu = menus.getMenu("main");;
                }
            }
        }
    }
};

