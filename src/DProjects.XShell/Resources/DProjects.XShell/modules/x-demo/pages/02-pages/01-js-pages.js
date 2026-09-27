// contract
export const contract = {
    description: "Pages javascript page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    dependencies: {
        code: "string:/_assets/x-demo/pages/02-pages/01-js-pages.js"
    },
    template: `
        <p>
            This is a sample js page
        </p>

        <pre>{{ state.content }}</pre>
    `,    
    state: {
        content: "This should be the content of the current file"
    },
    controller({ state, dependencies }) {
        return {
            load(params) {
               // load
               state.content = dependencies.code;
            }
        };
    }
}
