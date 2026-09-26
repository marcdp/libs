// contract
export const contract = {
    description: "Conditionals"
};

// export page
export default {
    template: `
       
        <button x-on:click="ready">Ready</button>
        <button x-on:click="working">Working</button>
        
        <div x-if="state.status == 'ready'">The structural branch says: Ready.</div>
        <div x-elseif="state.status == 'working'">The structural branch says: Working.</div>
        <div x-else>The structural branch says: another state.</div>
         
        <div>
            This element is visible: <p>{{ state.visible }}</p>
        </div>
 

        `,
    controller({ state }) {
        return {
            load() {
                // initialize demo state
                state.visible = true;
                state.status = "working";
            },
            ready() {
                state.status = "ready";               
            },
            working() {
                state.status = "working";
            },
            other() {
                state.status = "paused";
            }
        };
    }
};
