// export
export default class LoaderObjectString {
    async load(src) {
        // fetch 
        let response = await fetch(src);
        if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${src}`);
        return await response.text();
    }
};