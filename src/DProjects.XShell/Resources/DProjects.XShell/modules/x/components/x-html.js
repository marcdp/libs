

//utils
function encodeHTMLEntities(str) {
    return str.replace(/&/g, '&amp;')
              .replace(/</g, '&lt;')
              .replace(/>/g, '&gt;')
              .replace(/"/g, '&quot;')
              .replace(/'/g, '&#39;');
}
function colorize(html) {
    let result = [];
    let parts = html.split("<");
    result.push(`<span class="text">${encodeHTMLEntities(parts[0])}</span>`);
    for(let i = 1; i < parts.length; i++) {
        let part = parts[i];
        let j = part.indexOf(">");
        let tag = part;
        let text = "";
        if (j!=-1) {
            tag = part.substring(0, j);
            text = part.substring(j + 1);
        }
        tag = tag.replaceAll("\t", " ").replaceAll("\r", " ").replaceAll("\n", " ");
        let k = tag.indexOf(" ");
        if (k == -1) {
            result.push(`<span class="tag"'>&lt;${tag}&gt;</span>`);
        } else {
            result.push(`<span class="tag"'>&lt;`);
            let kPrev = 0;
            while (k!=-1) {
                let tagPart = tag.substring(kPrev, k);
                if (kPrev == 0){
                    result.push(`<span class="tag-name">${encodeHTMLEntities(tagPart)}</span>`);
                } else if (tagPart.indexOf("=") == -1) {
                    result.push(`<span class="attribute-name">${encodeHTMLEntities(tagPart)}</span>`);
                } else if (tagPart.length>0) {
                    let attrName = tagPart.substring(0, tagPart.indexOf("="));
                    let attrValue = tagPart.substring(tagPart.indexOf("=")+1);
                    result.push(`<span class="attribute-name">${encodeHTMLEntities(attrName)}</span>`);
                    result.push(`<span class="attribute-value">${encodeHTMLEntities(attrValue)}</span>`);
                }
                kPrev = k;
                k = tag.indexOf(" ", k + 1);
                if (kPrev != tag.length && k==-1) k = tag.length;
            }
            result.push(`&gt;</span>`);
        }
        result.push(`<span class="text">${encodeHTMLEntities(text)}</span>`);
    }
    return result.join("");
}

// export
export default {
    style:`
        :host {}
        pre {
            margin: 0;
            padding: 0;
            white-space: wrap;
            word-break: break-all;
        }
        .tag { color: blue; font-weight: bold}
        .tag-name { color: blue; font-weight: 600; }
        .attribute-name { color: red; font-weight:normal;}
        .attribute-value { color: green; font-weight:normal;}
        .text {color:gray}
    `,
    template: `
        <pre><code x-html="state.colorized"></code></pre>
    `,
    state: {
        value:    {value:"", attr:true},
        colorized:{value:""}
    },
    script({ state, events }) {
        return {
            onCommand(command, params){
                if (command == "load") {
                    //load
                    events.on(state, "change:value", (event) => {
                        state.colorized = colorize(event.newValue);                        
                    });
                } 
            }
        }
    }
}
