//export 
export default class LoaderIconSvg {
    async load (src) {
        // fetch svg and creates an SVGElement
        let response = await fetch(src);
        if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${src}`);
        let svg = await response.text();
        let div = document.createElement("div");
        div.innerHTML = svg;
        return div.firstChild;
    }
};