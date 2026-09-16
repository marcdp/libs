
// export
export default {
    style: `
        ul {margin:0; padding:0; display: flex; align-items:center; box-sizing: border-box;}
        ul li {list-style:none; margin-right:.5em; display:flex; }
        ul li.empty {display:none;}
        ul li x-anchor {position:relative; font-weight:500; }
        ul li x-anchor.selected:after {content:""; border-bottom:.1em var(--x-color-primary) solid; bottom:.1em; left:0; right:0; position:absolute;}
        x-icon.separator {color:var(--x-color-text-x-gray); margin-left:.35em; width:1em;}
        ul li:last-child {flex:1;}
        ul li:last-child x-icon.separator {visibility:hidden}

        @media (max-width: 768px) {
            ul {overflow:hidden; white-space: nowrap; flex:1;}
            ul li:first-child {flex-shrink:0;}
            ul li {flex-shrink:1;}
            x-anchor {display:inline;overflow:hidden; text-overflow: ellipsis;}
            x-icon.separator {margin-left:0; margin-right:-.35em;}
            ul li:last-child { flex-shrink:1; overflow:hidden;}
        }
    `,
    template: `
        <ul x-if="state.breadcrumb">
            <li x-for="item in state.breadcrumb" x-class:empty="!item.label">
                <x-anchor 
                    class="plain" 
                    x-attr:href="item.href"  
                    x-class:selected="(item!=state.breadcrumb[state.breadcrumb.length-1])" 
                    x-class:gray="item==state.breadcrumb[state.breadcrumb.length-1]"
                    open="auto"
                >
                    {{ item.label }}
                </x-anchor>
                <x-icon icon="x-keyboard-arrow-right" class="separator"><x-icon>
            </li>
        </ul>
    `,
    state: {
        breadcrumb: {value: []}
    },
    script({ events, bus, state, getPage }) {
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
                    const page  = getPage();
                    state.breadcrumb = page?.breadcrumb || [];
                }
            }
        }
    }
};

