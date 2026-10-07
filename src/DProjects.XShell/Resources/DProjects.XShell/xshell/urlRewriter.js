import { addRewriteRule } from "./utils/html.js";

// class
export default class UrlRewriter {

    // ctor
    constructor() {
    }

    // methods
    registerRule(tagName, attrName, type) {
        addRewriteRule(tagName, attrName, type);    
    }

}

