export default class LoaderResponseHttp {
    async load(src) {
        const response = await fetch(src);

        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}: ${src}`);
        }

        return response;
    }
}