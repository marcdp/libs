import {rewriteDocumentUrls} from "../utils/rewriteDocumentUrls.js";

// export
export class RenderEnginePlain {
	
	// ctor
	constructor({ host, template, state }){
		this._host = host;
		this._template = template.cloneNode(true);
		this._state = state;
	}

	// methods
	mount() {
		this._host.replaceChildren(this._template.content);
	}
	render() {
	}
	unmount() {
		this._host.replaceChildren();
	}


}
export default function createRenderEngineFactoryPlain(template, context) {
	// template
	const templateElement = document.createElement("TEMPLATE");
	templateElement.innerHTML = template;
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
			return new RenderEnginePlain({ host, template: templateElement, state });
		}
	};
}