// export
export default class LoaderObjectStringAny {
    async load(src) {
        // load text from the given source URL
        let response = await fetch(src);
        if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${src}`);
        return await response.text();
    }
};