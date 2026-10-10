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
        code: "string:/_assets/x-demo/1.0.0.dev/pages/02-pages/01-js-pages.js"
    },
    style: `
        P.p2 {
            border:1px blue solid; font-weight:bold;
            background-image: url("/img/random1.jpg");
        }
    `,
    template: `
        <p>
            This is a sample js page 1
        </p>

        <p class="p2">
            This is a sample js page 2
        </p>

        <pre>{{ state.content }}</pre>
    `,    
    state: {
        content: "This should be the content of the current file"
    },
    controller({ state, dependencies }) {
        return {
            load() {
               // load
               state.content = dependencies.code;
               
            }
        };
    }
}
