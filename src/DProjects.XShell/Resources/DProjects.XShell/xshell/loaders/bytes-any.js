export default class LoaderBytesAny {

    async load(src) {
        // load binary data from the given source URL
        const response = await fetch(src);
        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}: ${src}`);
        }
        return new Uint8Array(await response.arrayBuffer());
    }
}