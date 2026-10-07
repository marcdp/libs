import { processStyle } from "../utils/style.js";

// class
export default class LoaderStyleCss {

    // load and create the browser stylesheet
    async load(src, context) {
        const css = await processStyle({ src, context });
        const styleSheet = new CSSStyleSheet();
        await styleSheet.replace(css);
        return styleSheet;
    }
}
