// contract
export const contract = {
    description: "Editor components",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
    
        <h2>Code editor</h2>
        <x-datafield type="javascript" label="Javascript" x-prop:value="state.javascript"></x-datafield>
        <x-datafield type="html" label="Html" x-prop:value="state.html"></x-datafield>
        <x-datafield type="css" label="Css" x-prop:value="state.css"></x-datafield>
        <x-datafield type="xml" label="XML" x-prop:value="state.xml"></x-datafield>
        <x-datafield type="markdown" label="Markdown" x-prop:value="state.markdown"></x-datafield>

        <h2>Rich text</h2>

        <x-datafield
            type="richtext"
            label="Content"
            x-prop:value="state.richtext">
        </x-datafield>

        

    `,  
    state: {
        javascript: "alert(1);",
        html:"<p>Edit this <b>HTML</b> content.</p>",
        css: "<p>Edit this <b>CSS</b> content.</p>",
        xml: "<p>Edit this <b>XML</b> content.</p>",
        markdown: "<p>Edit this <b>Markdown</b> content.</p>",
        richtext: "hello world"
    },
    controller({ state }) {
        return {
            load() {
               // load
            }
        };
    }
}
