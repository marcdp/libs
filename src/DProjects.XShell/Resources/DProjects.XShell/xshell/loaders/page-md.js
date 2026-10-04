import Page from "../page.js";
import xshell from "../xshell.js";

export default class LoaderPageMd {

    async load(src, context) {
        // resolve the renderer through the normal component loader
        const componentMarkdown = xshell.config.xshell.ui.component.markdown;
        await xshell.loader.load("component:" + componentMarkdown);

        // return class
        return class extends Page {

            // ctor
            constructor({src, context}) {
                super({src, context});
                this._controller = {};
            }

            // methods
            async mount({host}) {
                if (this._unloaded) return;
                // mount the resolved document URL, independently of Page instance query state
                const markdown = document.createElement("x-markdown");
                markdown.setAttribute("src", src);
                host.replaceChildren(markdown);
                await super.mount({host});
            }

            async unmount() {
                if (this._unloaded) return;
                // detach Page-owned content before releasing the host
                this._host?.replaceChildren();
                await super.unmount();
            }
        };
    }
};
