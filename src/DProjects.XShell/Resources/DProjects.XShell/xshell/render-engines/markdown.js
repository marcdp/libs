import { marked } from "marked";
import { rewriteDocumentUrls } from "../utils/rewriteDocumentUrls.js";

// export
export class RenderEngineMarkdown {
	
	// ctor
	constructor({ host, templateElement, state }){
		this._host = host;
		this._templateElement = templateElement.cloneNode(true);
		this._state = state;
		this._dependencies = [];
	}

	// methods
	mount() {
		this._host.replaceChildren(this._templateElement.content);
	}
	render(){
	}
	unmount() {
		this._host.replaceChildren();
	}


}
export default function createRenderEngineFactoryMarkdown(template, context) {
	// convert to markdown
	const templateElement = document.createElement("TEMPLATE");
	templateElement.innerHTML = marked.parse(template);
	// dependencies
	const dependencies = new Set();
	templateElement.content.querySelectorAll("*").forEach(el => {
		if (el.tagName.includes("-")) {
			if (context.componentLazy && (el.localName != context.componentLazy && el.closest(context.componentLazy) != null)) return;
			dependencies.add("component:" + el.tagName.toLowerCase());
		}
	});
	// return
	return {
		dependencies: Object.freeze(Object.seal([...dependencies])),
		init: () => {
			rewriteDocumentUrls(templateElement.content, context);
		},
		create: ({host, state}) => {
			return new RenderEngineMarkdown({ host, templateElement, state });
		}
	};
}