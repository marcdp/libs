import {marked} from "../vendor/marked/marked.esm.js";

// export
export default function parse(markdown) {
    // Convert Markdown to HTML using the marked library
    return marked.parse(markdown);
}