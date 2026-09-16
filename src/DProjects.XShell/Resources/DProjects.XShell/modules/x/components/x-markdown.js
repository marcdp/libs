import XElement from "x-element";
import xshell from "xshell";
import {marked} from "marked";

// class
export default XElement.define("x-markdown", {
    style: `
        :host {display:block;}
    `,
    styleGlobal: `
        x-markdown { }
        x-markdown blockquote {display:block; border-left:.2em var(--x-color-gray) solid; background:#f0f0f0; margin:0; padding:0.05em; padding-left:1em; }
        x-markdown blockquote > p {margin:1em 0;}
        x-markdown code {
            padding: 2px 4px;
            font-size: 90%;
            color: #c7254e;
            background-color: #f9f2f4;
            border-radius: 4px;
        }
        x-markdown pre code {
            display:block;
            font-size: inherit;
            color: inherit;
            white-space: pre-wrap;
            background-color: #f8f8f8;
            border-radius: var(--x-datafield-border-radius);
            border: var(--x-datafield-border);
            padding: var(--x-datafield-padding);
        }
    `,
    template: `
        <slot></slot>
    `,
    state: {
        value:"",
        src:""
    },
    methods:{
        onCommand(name){
            if(name === "init") {
                //load
                this.state.addEventListener("change:value", async (event) => {
                    let html = marked.parse(event.newValue);
                    //load components
                    let docWithoutTemplate = (new DOMParser()).parseFromString(html.replace("<template>","<div>").replace("</template>","</div>"), "text/html");
                    let componentNames = [...new Set(Array.from(docWithoutTemplate.querySelectorAll('*')).filter(el => {return (el.tagName.includes('-'))}).map(el => "component:" + el.tagName.toLowerCase()))];
                    await xshell.loader.load(componentNames);
                    //set html
                    this.innerHTML = html;
                });
                this.state.addEventListener("change:src", async (event) => {
                    let src = event.newValue;
                    let response = await xshell.loader.load(src);
                    if (!response.ok) {
                        this.state.value = "404 - Not Found: " + src;
                        return;
                    }                        
                    this.state.value = await response.text();
                });
                
            }
        }
    }
});

