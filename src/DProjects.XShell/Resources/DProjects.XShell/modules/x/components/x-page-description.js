
// export
export default {
    style: ``,
    template: `{{ state.description }}`,
    state: {
        description: {value: ""},
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
                    state.description = page?.description || "";
                }
            }
        }
    }
};

