// export
export default class LoaderStyleCss {
    async load(src) {
        // fetch stylesheet
        let response = await fetch(src);
        if (!response.ok)
            throw new Error(`Error ${response.status}: ${response.statusText}: ${src}`);

        let css = await response.text();

        // create stylesheet
        let styleSheet = new CSSStyleSheet();
        await styleSheet.replace(css);

        return styleSheet;
    }
};