// contract
export const contract = {
    description: "Components page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the components page
        </p>

        <img src="/img/random1.jpg" style="width:10em;display:block;">

        <a href="01-basics.js">Basics A</a>
        <br/>
        <x-anchor href="01-basics.js">Basics x.anchor</x-anchor>
    `,    
    controller({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
