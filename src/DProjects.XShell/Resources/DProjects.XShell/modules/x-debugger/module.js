import xshell from "xshell";

// class
export default class XDebuggerModule {
    onCommand(command, params) {
        if (command == "load") {
            // register a menu source, which will provide with current app tabs as menu items
            xshell.menus.registerSource("x-debugger:tabs-menuitems", {
                dependsOn: ["xshell:tabs:changed"], // listen for event xshell:tabs:changed, to recompute menus
                resolve: () => {
                    return [
                        { label: "Tab XXXXXXXXXX1", href: "/pages/tab.html?tabId=XXXXXXXXXXXXXXXXXXXXXX1" },
                        { label: "Tab XXXXXXXXXX2", href: "/pages/tab.html?tabId=asdasd" },
                        { label: "Tab XXXXXXXXXX3", href: "/pages/tab.html?tabId=XXXXXXXXXXXXXXXXXXXXXX3" },
                        { label: "Tab XXXXXXXXXX4", href: "/pages/tab.html?tabId=XXXXXXXXXXXXXXXXXXXXXX4" },
                        { label: "Tab XXXXXXXXXX5", href: "/pages/tab.html?tabId=XXXXXXXXXXXXXXXXXXXXXX5" },
                    ];
                }
            });
        }
    }
};