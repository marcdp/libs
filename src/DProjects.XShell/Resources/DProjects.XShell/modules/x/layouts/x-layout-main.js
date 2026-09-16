import xshell from "xshell";
import { probablyPhone } from "../utils/dom.js";
import { findObjectsPath } from "../utils/object.js";


// class
export default {
    meta: {
        renderEngine: "x",
        stateEngine:  "proxy"
    },
    style:`
        :host {
            display:block;
            min-height:100vh;
        }
        x-loading {
            position:fixed;
            width:var(--x-loading-width);
            left:50%;
            top:var(--x-loading-top);
            transform:translateX(-50%);
            z-index:10;
        }
        .header {
            display:flex;
            align-items:center;
            padding:.45em;
            padding-left:1.2em;
            padding-right:1.5em;
            border-bottom:var(--x-layout-main-border);
            background:white;
            min-height:2.15em;
            position:relative;
        }
        .header .logo {display:block; }
        .header .logo img {display:block; width:3em; height:1.9em; object-fit:contain; border-right:var(--x-layout-main-border);}
        .header .spacer {flex:1}
        .header x-dropdown {margin-left:.5em; }
        
        .header.shell {z-index:6; padding-left:.65em;}

        .header.breadcrumb {z-index:5; padding-right:1.5em;}

        /* areas */
        .areas {color:var(--x-color-text); margin-left:.75em; margin-right:.5em; display:block;}
        .areas:hover > x-icon {color:var(--x-color-primary);}
        .areas:active > x-icon {color:var(--x-color-primary-dark);}

        /* search */
        .header .search {display:flex;}
        .header .search x-datafield {width:30em;}
        .header .search x-icon {transform:translate(.75em,.3em); position:absolute; color: var(--x-datafield-color-placeholder); position:0;}
        .header .search-button {display:none;}

        /* breadcrumb */
        .breadcrumb {position:sticky; top:0; user-select: none; flex:1;}
        
        .breadcrumb x-icon.toggle {border-radius:50%; font-size:1.225em; width:1.75em; height:1.75em; display:inline-flex; align-items:center; justify-content:center;line-height:1em; background:var(--x-color-primary); color:white; margin-right:.25em; cursor:pointer; margin-right:.5em}
        .breadcrumb x-icon.toggle:hover {background:var(--x-color-primary-dark); }
        .breadcrumb x-icon.toggle:active {background:var(--x-color-primary-x-dark)}
        
        .breadcrumb x-icon.toggle.toggled {background:none; color:var(--x-color-text)}
        .breadcrumb x-icon.toggle.toggled:hover {background:var(--x-color-xxxxx-gray); }
        .breadcrumb x-icon.toggle.toggled:active {background:var(--x-color-xxxx-gray)}

        /* menu */
        .body .menu {
            width: var(--x-layout-main-drawer-width);
            box-sizing: border-box;
            flex-shrink:0;            
            padding:1.3em;
            padding-left:2em;
        }
        .body .menu div {
            top:4em; 
            position: sticky;
            box-sizing: border-box;
        }
        .body .menu div x-button {
            position:absolute; 
            right:0
        }

        /* main */
        .body main {
            position:relative;
            box-sizing:border-box;
            padding: var(--x-layout-main-page-padding-vertical) var(--x-layout-main-page-padding-horizontal) var(--x-layout-main-page-padding-vertical) var(--x-layout-main-page-padding-horizontal);            
        }
        h1 {
            margin-top:0;
            font-size:var(--x-font-size-title);
            line-height:125%;
            font-weight:700;
        }
        h1 + x-page-description {
            font-size:var(--x-font-size-small);
            transform:translateY(-1em);
            display:block;
        }

        /* desktop */
        @media (min-width: 769px) {
            
            .body {
                display:flex;
                transition:margin var(--x-transition-duration);
                --x-fill-height: calc(100vh - 6.3em);
            }
            .body.toggled {
                margin-left: calc(var(--x-layout-main-drawer-width) * -1);	
            }
            .body .menu x-button[icon='x-close'] {
                display:none;
            }
            .body .divider {
                border-left: var(--x-layout-main-border);
                position:fixed; top:0; bottom:0;
                left: var(--x-layout-main-drawer-width);;
                transition:transform var(--x-transition-duration);
            }
            .body.toggled .divider {
                transform: translateX(calc(var(--x-layout-main-drawer-width) * -1));	
            }
            .body main { 
                flex:1;                
            }
            .body main > div {
                max-width:100em;
                margin-left:auto; 
                margin-right:auto;
            }
        }

        /* responsive */
        @media (max-width: 768px) {

            .header.shell {
                padding-left:.25em;
                padding-right:.75em;
            }
            .header .logo img {width:4em;}

            .header .search x-datafield {width:unset; flex:1;}
            .header .spacer  {display:none}
            .header .input-dropdown {flex:1;}

            .breadcrumb x-icon.toggle {background:none; color:var(--x-color-text)}
            .breadcrumb x-icon.toggle:hover {background:var(--x-color-xxxxx-gray); }
            .breadcrumb x-icon.toggle:active {background:var(--x-color-xxxx-gray)}

            .body {
                --x-fill-height: calc(100vh - 6.3em);
            }
            .body .menu {
                width: 100vw;
                background:var(--x-color-white);
                position:fixed;
                top:0em; 
                
                bottom:0;    
                transform:translateX(-100vw);
                transition:transform var(--x-transition-duration);
                border-right: var(--x-layout-main-border);
                z-index:10;
                padding: var(--x-layout-stack-page-padding-vertical) var(--x-layout-stack-page-padding-horizontal) var(--x-layout-stack-page-padding-vertical) var(--x-layout-stack-page-padding-horizontal);
            }
            .body .menu div {
                top:0; position:unset;
            }
            .body .menu div x-button {
                right:var(--x-layout-stack-page-padding-horizontal);
                top:calc(var(--x-layout-stack-page-padding-vertical) + .1em);
            }
            .body .menu x-button:not([icon='x-close']) {
                display:none;
            }
            .body.toggled .menu {
                transform:translateX(0);
                overflow-y:auto;
            }
            .body .divider {display:none;}

            }
    `,
    template: `
        <x-loading x-if="state.status=='loading'"></x-loading>        

        <!-- shell header -->
        <nav class="header shell">

            <a class="logo" x-attr:href="state.appBase" x-if="state.appIcon">
                <img x-attr:src="state.appIcon" x-attr:title="state.appLabel">
            </a>
            
            <x-dropdown class="areas popover">
                <x-icon tabindex="1" icon="x-apps" class="size-x2"></x-icon>
                <x-page slot="dropdown" src="/pages/areas.js" loading="lazy"></x-page>
            </x-dropdown>

            <x-dropdown class="input-dropdown">
                <div class="search">
                    <x-datafield type="search" placeholder="Search" x-model="state.keyword" ></x-datafield>
                </div>
                <x-page slot="dropdown" src="/pages/search.js" loading="lazy"></x-page>
            </x-dropdown>

            <div class="spacer"></div>

            <x-dropdown class="popover left" collapse-on-click>
                <x-button x-attr:label="state.userInitials" x-attr:title="state.userName" class="round light"></x-button>
                <x-page slot="dropdown" src="/pages/identity.js" loading="lazy"></x-page>
                <x-menu slot="dropdown" x-prop:menu="state.menuProfile"></x-menu>
            </x-dropdown>

        </nav>

        <!-- breadcrumb -->
        <nav class="header breadcrumb">
            <div x-if="state.menuMain">
                <x-icon icon="x-menu" x-on:click="toggle-menu" class="toggle" x-class:toggled="state.toggled"></x-icon>
            </div>
            <x-page-breadcrumb></x-page-breadcrumb>

            <div class="spacer"></div>

            <x-menu x-prop:menu="state.menuTools" class="horizontal"></x-menu>            
        </nav>

        <!-- body -->
        <div class="body" x-class:toggled="state.toggled">
            <nav class="menu">
                <div>
                    <x-button class="anchor" icon="x-keyboard-arrow-left" x-on:click="toggle-menu"></x-button>
                    <x-button class="anchor" icon="x-close" x-on:click="toggle-menu"></x-button>
                    <x-page-menu></x-page-menu>
                    <x-menumain x-prop:menu="state.menuMain"></x-menumain>
                </div>
            </nav>
            <div class="divider"></div>
            <main>           
                <div>     
                    <h1>
                        <x-page-title></x-page-title>
                    </h1>
                    <x-page-description></x-page-description>
                    <slot></slot>
                </div>
            </main>
        </div>

    `,
    state: {
        status:         {value: "", attr:true},
        appIcon:        {value: xshell.config.get("app.icon")},
        appLabel:       {value: xshell.config.get("app.label")},
        appBase:        {value: xshell.config.get("app.base")},
        userName:       {value: ""},
        userInitials:   {value: ""},
        menuMain:       {value: null},
        menuTools:      {value: null},
        menuProfile:    {value: null},
        toggled:        {value: false},
        keyword:        {value: ""},
        shellDebug:     {value: xshell.config.get("xshell.debug")}
    },
    script({ state, events, navigation, modules, menus, bus, getPage }) {
        return {
            async onCommand(command, params) {
                if (command == "load") {
                    //load
                    state.toggled = false; //xshell.settings.getItem("x-layout-main.toggled", false);
                    state.userName = xshell.identity.name;
                    state.userInitials =  (() => { let w = xshell.identity.name.trim().split(/\s+/); return (w.length > 1 ? w[0][0] + w.at(-1)[0] : w[0].slice(0,2)); })().toUpperCase();
                    // auto close menu on navigation start (mobile)
                    events.on(bus, "xshell:navigation:start", (e) => {
                        if (probablyPhone()) {
                            if (state.toggled) {
                                state.toggled = false;
                            }
                        }
                    });
                    events.on(bus, "xshell:navigation:end", "refresh");
                    // bind refresh
                    events.on(bus, "xshell:menus:changed", "refresh")
                    // bind search
                    events.on(state, "change:keyword", "search");
                    
                } else if (command == "mount") {
                    // refresh
                    this.onCommand("refresh");

                } else if (command == "refresh") {
                    //refresh
                    const page  = getPage();
                    if (page) {
                        let module = modules.resolveModuleName(page.src);
                        if (!module) {
                            //no module defined 
                            state.menuMain = null;
                        } else if (page.breadcrumb && page.breadcrumb.length){
                            //get module
                            module = modules.resolveModuleName(page.src);
                            //show menu main and tools
                            state.menuMain = menus.getMenu("main");
                            state.menuTools = menus.getMenu("tools");
                            state.menuProfile = menus.getMenu("profile");
                        } else { 
                            // not found breadcrumb in page
                            // show menu main and tools
                            state.menuMain = menus.getMenu("main");
                            state.menuTools = menus.getMenu("tools");
                            state.menuProfile = menus.getMenu("profile");
                            //breadcrumb
                            if (state.menuMain) {
                                let menuitems = findObjectsPath(state.menuMain, 'href', page.href);
                            }
                        }
                    }

                } else if (command == "menuitem-clicked") {
                    //menuitem-clicked
                    alert("menuitem-clicked");

                } else if (command == "toggle-menu") {
                    //toggle-menu
                    state.toggled = !state.toggled;
                    settings.setItem("x-layout-main.toggled", state.toggled);

                } else if (command == "search") {
                    //search
                    bus.emit("xshell:search", { keyword: state.keyword.trim() });
                }
                
            }
        };
    }
}
