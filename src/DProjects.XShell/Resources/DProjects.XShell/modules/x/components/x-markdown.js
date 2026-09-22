import {marked} from "marked";


// contract
export const contract = {
    description: "Renders Markdown text or Markdown loaded from a source URL.",
    events: {},
    properties: {
        value: {type:"string", default:"", attr:true, state:true, description:""},
        src:   {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `,
    state: {
        value: {value:"", type:"string", attr:true, prop:true},
        src:   {value:"", type:"string", attr:true, prop:true}
    },
    script({ state, events, loader }) {
        return {
            onCommand(name){
                if(name === "load") {
                    //load
                    events.on(state, "change:value", async (event) => {
                        let html = marked.parse(event.newValue);
                        //load components
                        let docWithoutTemplate = (new DOMParser()).parseFromString(html.replace("<template>","<div>").replace("</template>","</div>"), "text/html");
                        let componentNames = [...new Set(Array.from(docWithoutTemplate.querySelectorAll('*')).filter(el => {return (el.tagName.includes('-'))}).map(el => "component:" + el.tagName.toLowerCase()))];
                        await loader.load(componentNames);
                        //set html
                        this.innerHTML = html;
                    });
                    events.on(state, "change:src", async (event) => {
                        let src = event.newValue;
                        let response = await loader.load(src);
                        if (!response.ok) {
                            state.value = "404 - Not Found: " + src;
                            return;
                        }
                        state.value = await response.text();
                    });
                }
            }
        };
    }
};

