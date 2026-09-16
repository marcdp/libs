// export
export default {
    style: `
        :host {border:1px solid black; display:inline-block; padding:10px;}
    `,
    template: `
        {{ state.time }}
    `,
    state: {
        time: {value:null, type:"date"}
    },
    script: ({ state, timer }) => {
        return {
            onCommand(command) {
                if (command == "load") {
                    //load
                    this.onCommand("refresh");
                    timer.setInterval(1000, "refresh");

                } else if (command == "refresh") {
                    //refresh
                    state.time = new Date().toLocaleTimeString();
                }
            }
        };
    }
};
