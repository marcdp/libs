// contract
export const contract = {
    description: "Pages javascript page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            This is a sample js page
        </p>

        <pre>{{ state.content }}</pre>
    `,    
    state: {
        content: "This should be the content of the current file"
    },
    controller({ state }) {
        return {
            load(params) {
               // load
               // TODO 
               // state.content = ....
            }
        };
    }
}
